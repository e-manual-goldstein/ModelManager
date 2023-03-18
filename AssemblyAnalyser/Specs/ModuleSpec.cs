using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
namespace AssemblyAnalyser
{
    public class ModuleSpec : AbstractSpec
    {
        ModuleDefinition _module;
        public ModuleSpec(ModuleDefinition module, string filePath,
             ISpecManager specManager, List<IRule> rules) : this(module.Assembly.FullName, specManager, rules)
        {
            _module = module;
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

        ModuleSpec[] _referencedAssemblies;

        public ModuleSpec[] ReferencedModules => _referencedAssemblies ??= LoadReferencedModules();

        TypeSpec[] _typeSpecs;
        public TypeSpec[] TypeSpecs => _typeSpecs ??= _specManager.TryLoadTypesForModule(_module);
        
        protected override CustomAttribute[] GetAttributes()
        {
            return _module.CustomAttributes.ToArray();
        }

        public ModuleSpec[] LoadReferencedModules(bool includeSystem = false)
        {
            return (_referencedAssemblies ??= _specManager.LoadReferencedModules(_module))
                .Where(r => !r.IsSystem || includeSystem).ToArray();
        }

        protected override void BuildSpec()
        {
            foreach (var referencedModule in ReferencedModules)
            {
                referencedModule.Process();
                referencedModule.RegisterAsReferencedAssemblyFor(this);
            }
            _typeSpecs = _specManager.TryLoadTypesForModule(_module);
        }

        public override string ToString()
        {
            return ModuleFullName;
        }

        internal TypeDefinition GetTypeDefinition(TypeReference typeReference)
        {
            return _module.GetType(typeReference.FullName);
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
    }
}
