using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AssemblyAnalyser.Specs;
using Mono.Cecil;
namespace AssemblyAnalyser
{
    public class ModuleSpec : AbstractSpec
    {
        //the _baseVersion is the version of the module for which an assembly has been located
        //this is the version which corresponds to the file found at FilePath
        ModuleDefinition _baseVersion;
        public ModuleSpec(ModuleDefinition module, string filePath,
             ISpecManager specManager) : this(module.Assembly.FullName, specManager)
        {
            AddSearchDirectory(module, filePath);
            Versions = new();
            _baseVersion = module;
            Versions.Add(module.Assembly.FullName, module.Assembly.Name);
            ModuleShortName = module.Assembly.Name.Name;
            FilePath = filePath;
            IsSystem = AssemblyLoader.IsSystemAssembly(filePath);
        }

        protected ModuleSpec(string assemblyFullName, ISpecManager specManager) 
            : base(specManager)
        {
            _specManager = specManager;
            ModuleFullName = assemblyFullName;
        }

        private void AddSearchDirectory(ModuleDefinition module, string filePath)
        {
            if (module.AssemblyResolver is DefaultAssemblyResolver defaultAssemblyResolver)
            {
                if (!defaultAssemblyResolver.GetSearchDirectories().Contains(Path.GetDirectoryName(filePath)))
                {
                    defaultAssemblyResolver.AddSearchDirectory(Path.GetDirectoryName(filePath));
                }
            }
        }

        public string ModuleShortName { get; protected set; }
        public string FilePath { get; }
        
        public string ModuleFullName { get; }

        protected Dictionary<string, AssemblyNameReference> Versions { get; set; }
        
        public void AddModuleVersion(AssemblyNameReference reference)
        {
            if (!Versions.ContainsKey(reference.Version.ToString()))
            {
                Versions[reference.Version.ToString()] = reference;
            }
        }

        ModuleSpec[] _referencedAssemblies;

        public ModuleSpec[] ReferencedModules => _referencedAssemblies ??= LoadReferencedModules();

        TypeSpec[] _typeSpecs;
        public TypeSpec[] TypeSpecs => _typeSpecs ??= _specManager.TryLoadTypesForModule(_baseVersion);
        
        protected override CustomAttribute[] GetAttributes()
        {
            return _baseVersion.CustomAttributes.ToArray();
        }

        public ModuleSpec[] LoadReferencedModules(bool includeSystem = false)
        {
            return (_referencedAssemblies ??= _specManager.LoadReferencedModules(_baseVersion))
                .Where(r => !r.IsSystem || includeSystem).ToArray();
        }

        protected override void BuildSpec()
        {
            foreach (var referencedModule in ReferencedModules)
            {
                referencedModule.Process();
                referencedModule.RegisterAsReferencedAssemblyFor(this);
            }
            _typeSpecs = _specManager.TryLoadTypesForModule(_baseVersion);
        }

        public override string ToString()
        {
            return ModuleFullName;
        }

        public TypeDefinition GetTypeDefinition(TypeReference typeReference)
        {
            var types = _baseVersion.Types.ToArray();
            return _baseVersion.GetType(typeReference.FullName);
        }

        public GenericParameter GetGenericParameter(TypeReference typeReference)
        {
            var typesWithGenericParameters = _baseVersion.Types.Where(t => t.HasGenericParameters).ToArray();
            if (typeReference.DeclaringType != null)
            {
                var matchingType = typesWithGenericParameters.SingleOrDefault(t => t.FullName == typeReference.DeclaringType.FullName);
                return matchingType.GenericParameters.SingleOrDefault(t => t.FullName == typeReference.FullName);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        List<GenericInstanceSpec> _genericTypeImplementations = new List<GenericInstanceSpec>();

        public GenericInstanceSpec[] GenericTypeImplementations => _genericTypeImplementations.ToArray();

        public void AddGenericTypeImplementation(GenericInstanceSpec genericInstance)
        {
            if (TypeSpecs.Contains(genericInstance))
            {

            }
            if (!_genericTypeImplementations.Contains(genericInstance))
            {
                _genericTypeImplementations.Add(genericInstance);
            }
        }

        List<TypeSpec> _dependentTypes = new List<TypeSpec>();

        public TypeSpec[] DependentTypes => _dependentTypes.ToArray();

        public void RegisterDependentType(TypeSpec typeSpec)
        {
            if (!_dependentTypes.Contains(typeSpec))
            {
                _dependentTypes.Add(typeSpec);
            }
        }

        List<ModuleSpec> _referencedBy = new List<ModuleSpec>();

        public ModuleSpec[] ReferencedBy => _referencedBy.ToArray();

        private void RegisterAsReferencedAssemblyFor(ModuleSpec assemblySpec)
        {
            if (!_referencedBy.Contains(assemblySpec))
            {
                _referencedBy.Add(assemblySpec);
            }
        }

        public TypeSpec GetTypeSpec(TypeReference typeReference)
        {
            var matchingByName = TypeSpecs.Where(t => t.IsSpecFor(typeReference, true)).ToList();
            return matchingByName.Single();
        }

        public TypeSpec GetTypeSpec(string fullTypeName)
        {
            var matchingByName = TypeSpecs.Where(t => t.FullTypeName == fullTypeName).ToList();
            return matchingByName.SingleOrDefault();
        }

        public bool HasScopeName(string name)
        {
            return _baseVersion.Assembly.Name.Name == name;
        }

        public bool IsSpecFor(TypeReference typeReference)
        {
            throw new NotImplementedException();
            //return _baseVersion.HasTypeReference(definition.Scope.Name, definition.FullName);
            //return _baseVersion.Types.Any(t 
            //    => t.FullName == typeReference.FullName
            //    && t.Scope == typeReference.Scope);
            //return _baseVersion.HasTypeReference(typeReference.Scope.Name, typeReference.FullName);
        }
    }
}
