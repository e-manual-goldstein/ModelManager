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

namespace AssemblyAnalyser
{
    public class AssemblySpec : AbstractSpec
    {
        AssemblyDefinition _assemblyDefinition;

        public AssemblySpec(AssemblyDefinition assemblyDefinition, string filePath, ISpecManager specManager) : this(assemblyDefinition.FullName, specManager)
        {
            _assemblyDefinition = assemblyDefinition;
            AssemblyShortName = assemblyDefinition.Name.GetScopeNameWithoutExtension();
            AssemblyFullName = assemblyDefinition.FullName;
            FilePath = filePath;
            IsSystemAssembly = AssemblyLocator.IsSystemAssembly(filePath);
        }

        protected AssemblySpec(string assemblyFullName, ISpecManager specManager)
            : base(specManager)
        {
            AssemblyFullName = assemblyFullName;
        }

        public string AssemblyFullName { get; }
        public string AssemblyShortName { get; protected set; }
        public string FilePath { get; set; }
        public bool IsSystemAssembly { get; }
        public string TargetFrameworkVersion { get; private set; }
        public string ImageRuntimeVersion { get; private set; }

        #region Module Specs

        ModuleSpec[] _modules;
        public ModuleSpec[] Modules => _modules ??= TryGetModuleSpecs(_assemblyDefinition.Modules.ToArray());
        
        ConcurrentDictionary<string, ModuleSpec> _moduleSpecs = new ConcurrentDictionary<string, ModuleSpec>();

        public virtual ModuleSpec LoadModuleSpec(IMetadataScope scope)
        {
            return _moduleSpecs.GetOrAdd(scope.GetScopeNameWithoutExtension(), (key) => CreateFullModuleSpec(scope));
        }

        public ModuleSpec LoadModuleSpecForTypeReference(TypeReference typeReference)
        {
            if (typeReference == null)
            {
                throw new NotImplementedException();
            }
            if (SystemModuleSpec.IsSystemModule(typeReference.Scope))
            {
                return _moduleSpecs.GetOrAdd(SystemModuleSpec.GetSystemModuleName(typeReference.Scope),
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
            if (typeReference.Resolve() is TypeDefinition typeDefinition)
            {
                return _moduleSpecs.GetOrAdd(typeDefinition.Scope.GetScopeNameWithoutExtension(),
                    (key) => CreateFullModuleSpec(typeDefinition.Scope));
            }
            return _moduleSpecs.GetOrAdd(typeReference.Scope.GetScopeNameWithoutExtension(),
                (key) => CreateFullModuleSpec(typeReference.Scope));
        }

        public ModuleSpec LoadModuleSpecFromPath(string moduleFilePath)
        {
            var assembly = _specManager.LoadAssemblySpecFromPath(moduleFilePath);
            
            var readerParameters = new ReaderParameters()
            {
                AssemblyResolver = _specManager.AssemblyResolver
            };
            var moduleDefinition = ModuleDefinition.ReadModule(moduleFilePath, readerParameters);
            return LoadModuleSpec(moduleDefinition);
        }

        public ModuleSpec[] LoadReferencedModules(ModuleDefinition baseModule)
        {
            var specs = new List<ModuleSpec>();
            foreach (var assemblyReference in baseModule.AssemblyReferences)
            {
                var moduleSpec = LoadReferencedModuleByFullName(baseModule, assemblyReference.FullName);
                if (moduleSpec != null)
                {
                    specs.Add(moduleSpec);
                }
            }
            return specs.OrderBy(s => s.FilePath).ToArray();
        }

        public ModuleSpec LoadReferencedModuleByFullName(ModuleDefinition module, string referencedModuleName)
        {
            if (module.Name == referencedModuleName)
            {
                return LoadModuleSpec(module);
            }
            var locator = AssemblyLocator.GetLocator(module);
            var assemblyReference = module.AssemblyReferences.Single(a => a.FullName.Contains(referencedModuleName));
            return LoadReferencedModule(locator, assemblyReference);
        }

        public ModuleSpec LoadReferencedModuleByScopeName(ModuleDefinition module, IMetadataScope scope)
        {
            if (module.GetScopeNameWithoutExtension() == scope.GetScopeNameWithoutExtension())
            {
                return LoadModuleSpec(module);
            }
            var locator = AssemblyLocator.GetLocator(module);
            var version = scope switch
            {
                AssemblyNameReference assemblyNameReference => assemblyNameReference.Version,
                ModuleDefinition moduleDefinition => moduleDefinition.Assembly.Name.Version,
                _ => throw new NotImplementedException()
            };
            var assemblyReference = module.AssemblyReferences
                .Single(a => a.FullName.ParseShortName() == scope.GetScopeNameWithoutExtension() && a.Version == version);
            return LoadReferencedModule(locator, assemblyReference);
        }

        private ModuleSpec LoadReferencedModule(AssemblyLocator locator, AssemblyNameReference assemblyReference)
        {
            var assemblyLocation = locator.LocateAssemblyByName(assemblyReference.FullName);
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
            if (scope is ModuleDefinition moduleDefinition)
            {
                return new ModuleSpec(moduleDefinition, moduleDefinition.FileName, _specManager);
            }
            return CreateMissingModuleSpec(scope as AssemblyNameReference);
        }

        protected ModuleSpec LoadModuleByAssemblyNameReference(AssemblyNameReference assemblyNameReference)
        {
            return LoadModuleSpec(assemblyNameReference);
        }

        protected ModuleSpec CreateMissingModuleSpec(AssemblyNameReference assemblyNameReference)
        {
            var spec = new MissingModuleSpec(assemblyNameReference, _specManager);
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
        //    _moduleSpecs = TryGetModuleSpecs(_assemblyDefinition.Modules.ToArray());

        }

        private ModuleSpec[] TryGetModuleSpecs(ModuleDefinition[] moduleDefinitions)
        {
            return LoadModuleSpecs(moduleDefinitions);            
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _assemblyDefinition.CustomAttributes.ToArray();
        }

        public override string ToString()
        {
            return AssemblyFullName;
        }
    }
}
