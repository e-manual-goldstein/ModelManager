using Mono.Cecil;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AssemblyAnalyser.Extensions;
using System;
using AssemblyAnalyser.Specs;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using StaticAssemblyLocator = AssemblyAnalyser.AssemblyLocator;

namespace AssemblyAnalyser
{
    public class AssemblySpec : AbstractSpec
    {
        protected readonly AssemblyDefinition _assemblyDefinition;
        private readonly IAssemblyLocator _assemblyLocator;

        public AssemblySpec(AssemblyDefinition assemblyDefinition, string filePath, IAssemblyLocator assemblyLocator, ISpecManager specManager) : this(assemblyDefinition.FullName, specManager)
        {
            _assemblyDefinition = assemblyDefinition;
            _assemblyLocator = assemblyLocator;
            AssemblyShortName = assemblyDefinition.Name.GetScopeNameWithoutExtension();
            Versions.Add(assemblyDefinition.Name.GetUniqueNameFromScope());
            AssemblyFullName = assemblyDefinition.FullName;
            FilePath = filePath;
            IsSystem = StaticAssemblyLocator.IsSystemAssembly(filePath);//Should be no need for this eventually;
        }

        protected AssemblySpec(string assemblyFullName, ISpecManager specManager)
            : base(specManager)
        {
            AssemblyFullName = assemblyFullName;
        }

        public List<string> Versions { get; set; } = new();

        public string AssemblyFullName { get; }
        public string AssemblyShortName { get; protected set; }
        public string FilePath { get; set; }
        
        public IAssemblyLocator AssemblyLocator => _assemblyLocator;

        string _targetFrameworkVersion;
        public string TargetFrameworkVersion => _targetFrameworkVersion ??= TryGetTargetFrameworkVersion();

        protected virtual string TryGetTargetFrameworkVersion()
        {
            var targetFrameworkAttribute = _assemblyDefinition.CustomAttributes
                .SingleOrDefault(t => t.AttributeType.FullName == typeof(TargetFrameworkAttribute).FullName);
            if (targetFrameworkAttribute != null)
            {
                var ctorArgument = targetFrameworkAttribute.ConstructorArguments.SingleOrDefault();
                return $"{ctorArgument.Value}";
            }
            return string.Empty;
        }

        string _dotNetVersion;
        public string DotNetPlatformVersion => _dotNetVersion ??= TryGetDotNetPlatformVersion();

        private string TryGetDotNetPlatformVersion()
        {
            if (!string.IsNullOrEmpty(TargetFrameworkVersion))
            {
                var match = Regex.Match(TargetFrameworkVersion, "(?'PlatformVersion'.NETCoreApp|.NETStandard|.NETFramework),(?'Suffix'.*)");
                return match.Success ? match.Groups["PlatformVersion"].Value : string.Empty;
            }
            return string.Empty;
        }

        public string ImageRuntimeVersion { get; private set; }

        #region Module Specs

        ModuleSpec[] _modules;
        public ModuleSpec[] Modules => _modules ??= TryGetModuleSpecs();
        
        protected ConcurrentDictionary<string, ModuleSpec> _moduleSpecs = new ConcurrentDictionary<string, ModuleSpec>();

        public virtual ModuleSpec LoadModuleSpec(IMetadataScope scope)
        {
            return _moduleSpecs.GetOrAdd(scope.GetScopeNameWithoutExtension(), (key) => CreateFullModuleSpec(scope));
        }

        public virtual ModuleSpec LoadModuleSpecForTypeReference(TypeReference typeReference)
        {
            if (typeReference == null)
            {
                throw new NotImplementedException();
            }
            if (SystemModuleSpec.IsSystemModule(typeReference.Scope))
            {
                return _moduleSpecs.GetOrAdd(SystemAssemblySpec.SYSTEM_MODULE_NAME,
                    (key) => CreateFullModuleSpec(typeReference.Scope));
            }
            if (typeReference.IsGenericInstance)
            {
                return _moduleSpecs.GetOrAdd(typeReference.Module.Name,
                    (key) => CreateFullModuleSpec(typeReference.Module));
            }
            if (_moduleSpecs.TryGetValue(typeReference.Scope.GetScopeNameWithoutExtension(), out ModuleSpec scopeModuleSpec))
            {
                return scopeModuleSpec;
            }
            if (_moduleSpecs.TryGetValue(typeReference.Module.GetScopeNameWithoutExtension(), out scopeModuleSpec))
            {
                return scopeModuleSpec;
            }
            if (typeReference is TypeDefinition typeDefinition)
            {
                return _moduleSpecs.GetOrAdd(typeDefinition.Scope.GetScopeNameWithoutExtension(),
                    (key) => CreateFullModuleSpec(typeDefinition.Scope));
            }
            return _moduleSpecs.GetOrAdd(typeReference.Scope.GetScopeNameWithoutExtension(),
                (key) => CreateFullModuleSpec(typeReference.Scope));
        }

        public ModuleSpec LoadModuleSpecFromPath(string moduleFilePath)
        {
            //var assembly = _specManager.LoadAssemblySpecFromPath(moduleFilePath);
            
            var readerParameters = new ReaderParameters()
            {
                AssemblyResolver = _specManager.AssemblyResolver
            };
            var moduleDefinition = ModuleDefinition.ReadModule(moduleFilePath, readerParameters);
            return LoadModuleSpec(moduleDefinition);
        }

        public IEnumerable<ModuleSpec> LoadReferencedModules(ModuleDefinition baseModule)
        {
            foreach (var assemblyReference in baseModule.AssemblyReferences)
            {
                yield return LoadReferencedModuleByFullName(baseModule, assemblyReference.FullName);                
            }
        }

        public ModuleSpec LoadReferencedModuleByFullName(ModuleDefinition module, string referencedModuleName)
        {
            if (module.Name == referencedModuleName)
            {
                return LoadModuleSpec(module);
            }
            var assemblyReference = module.AssemblyReferences.Single(a => a.FullName.Contains(referencedModuleName));
            return LoadReferencedModule(assemblyReference);
        }

        public ModuleSpec LoadReferencedModuleByScopeName(ModuleDefinition module, IMetadataScope scope)
        {
            if (module.GetScopeNameWithoutExtension() == scope.GetScopeNameWithoutExtension())
            {
                return LoadModuleSpec(module);
            }
            var version = scope switch
            {
                AssemblyNameReference assemblyNameReference => assemblyNameReference.Version,
                ModuleDefinition moduleDefinition => moduleDefinition.Assembly.Name.Version,
                _ => throw new NotImplementedException()
            };
            var assemblyReference = module.AssemblyReferences
                .Single(a => a.FullName.ParseShortName() == scope.GetScopeNameWithoutExtension() && a.Version == version);
            return LoadReferencedModule(assemblyReference);
        }

        private ModuleSpec LoadReferencedModule(AssemblyNameReference assemblyReference)
        {
            var assemblyLocation = AssemblyLocator.LocateAssemblyByName(assemblyReference.FullName);
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                var missingModuleSpec = _moduleSpecs.GetOrAdd(assemblyReference.Name, (key) => CreateMissingModuleSpec(assemblyReference));
                missingModuleSpec.AddModuleVersion(assemblyReference);
                return missingModuleSpec;
            }
            var moduleSpec = LoadModuleSpecFromPath(assemblyLocation);
            moduleSpec.AddModuleVersion(assemblyReference);
            return moduleSpec;
        }

        public ModuleSpec[] LoadModuleSpecs(ModuleDefinition[] modules)
        {
            return modules.Select(t => LoadModuleSpec(t)).ToArray();
        }

        protected virtual ModuleSpec CreateFullModuleSpec(IMetadataScope scope)
        {
            var moduleDefinition = scope as ModuleDefinition ?? GetModuleForScope(scope);
            if (moduleDefinition != null)
            {
                return new ModuleSpec(moduleDefinition, moduleDefinition.FileName, this, _specManager);
            }
            return CreateMissingModuleSpec(scope as AssemblyNameReference);
        }

        protected ModuleDefinition GetModuleForScope(IMetadataScope scope)
        {
            if (_assemblyDefinition.Modules.Count == 1)
            {
                return _assemblyDefinition.Modules[0];
            }
            return _assemblyDefinition.Modules.Where(m => m.GetScopeNameWithoutExtension() == scope.GetScopeNameWithoutExtension()).SingleOrDefault();
        }

        protected ModuleSpec LoadModuleByAssemblyNameReference(AssemblyNameReference assemblyNameReference)
        {
            return LoadModuleSpec(assemblyNameReference);
        }

        protected MissingModuleSpec CreateMissingModuleSpec(AssemblyNameReference assemblyNameReference)
        {
            var spec = new MissingModuleSpec(assemblyNameReference, this, _specManager);
            return spec;
        }

        #endregion

        //ModuleSpec[] _moduleSpecs;
        //public ModuleSpec[] ModuleSpecs => _moduleSpecs ??= TryGetModuleSpecs(_assemblyDefinition.Modules.ToArray());

        //IDictionary<ModuleSpec, AssemblySpec[]> _referencedAssemblies;
        //public IDictionary<ModuleSpec, AssemblySpec[]> ReferencedAssemblies => _referencedAssemblies ??= LoadReferencedAssemblies();

        //private IDictionary<ModuleSpec, AssemblySpec[]> LoadReferencedAssemblies()
        //{
        //    return ModuleSpecs.ToDictionary(f => f, g =>
        //    {
        //        return g.LoadReferencedAssemblies(true);
        //    });
        //}

        protected override void BuildSpec()
        {
            
        }

        protected virtual ModuleSpec[] TryGetModuleSpecs()
        {
            return LoadModuleSpecs(_assemblyDefinition.Modules.ToArray());
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _assemblyDefinition.CustomAttributes.ToArray();
        }

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this, AssemblyLocator);
        }

        public override string ToString()
        {
            return AssemblyFullName;
        }

        public virtual AssemblySpec RegisterMetaDataScope(IMetadataScope assemblyNameReference)
        {
            return this;
        }
    }
}
