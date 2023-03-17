using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
namespace AssemblyAnalyser
{
    public class ModuleSpec : AbstractSpec
    {
        ModuleDefinition _module;
        public ModuleSpec(ModuleDefinition module, string filePath,
             ISpecManager specManager, List<IRule> rules) : this(module.Name, specManager, rules)
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
    }
}
