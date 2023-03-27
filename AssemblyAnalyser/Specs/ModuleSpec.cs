using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
namespace AssemblyAnalyser
{
    public class ModuleSpec : AbstractSpec
    {
        //the _baseVersion is the version of the module for which an assembly has been located
        //this is the version which corresponds to the file found at FilePath
        ModuleDefinition _baseVersion;
        public ModuleSpec(ModuleDefinition module, string filePath,
             ISpecManager specManager, List<IRule> rules) : this(module.Assembly.FullName, specManager, rules)
        {
            Versions = new();
            _baseVersion = module;
            Versions.Add(module.Assembly.FullName, module.Assembly.Name);
            ModuleShortName = module.Assembly.Name.Name;
            FilePath = filePath;
            IsSystem = AssemblyLoader.IsSystemAssembly(filePath);
        }

        ModuleSpec(string assemblyFullName, ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
        {
            //_representedModuleNames.Add(assemblyFullName);
            _specManager = specManager;
            ModuleFullName = assemblyFullName;
        }
                
        public string ModuleShortName { get; }
        public string FilePath { get; }
        public bool IsSystem { get; }
        public string ModuleFullName { get; }

        Dictionary<string, AssemblyNameReference> Versions { get; }
        
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

        internal TypeDefinition GetTypeDefinition(TypeReference typeReference)
        {
            return _baseVersion.GetType(typeReference.FullName);
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
            var matchingByName = TypeSpecs.Where(t => t.IsSpecFor(typeReference)).ToList();
            return matchingByName.Single();
        }

        public bool HasScopeName(string name)
        {
            return _baseVersion.Assembly.Name.Name == name;
        }

        public bool IsSpecFor(TypeReference typeReference)
        {
            var definition = typeReference.Resolve();
            return _baseVersion.HasTypeReference(definition.Scope.Name, definition.FullName);
            //return _baseVersion.Types.Any(t 
            //    => t.FullName == typeReference.FullName
            //    && t.Scope == typeReference.Scope);
            //return _baseVersion.HasTypeReference(typeReference.Scope.Name, typeReference.FullName);
        }
    }
}
