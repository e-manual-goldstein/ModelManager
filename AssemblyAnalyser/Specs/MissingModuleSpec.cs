using Mono.Cecil;
using System;

namespace AssemblyAnalyser.Specs
{
    public class MissingModuleSpec : ModuleSpec
    {
        AssemblyNameReference _missingAssembly;

        public MissingModuleSpec(AssemblyNameReference missingAssembly, ISpecManager specManager) 
            : base(missingAssembly.FullName, specManager)
        {
            _missingAssembly = missingAssembly;
            Versions = new();
            ModuleShortName = missingAssembly.Name;
            Versions.Add(missingAssembly.FullName, missingAssembly);
            specManager.AddFault(FaultSeverity.Warning, $"Asssembly not found {missingAssembly.FullName}");
        }

        public AssemblyNameReference MissingAssembly => _missingAssembly;

        public override bool IsSystem { get => base.IsSystem; protected set => base.IsSystem = value; }

        public override void RegisterAsRequiredBy(ISpecDependency specDependency)
        {
            base.RegisterAsRequiredBy(specDependency);
        }

        public override void RegisterDependency(ISpecDependency specDependency)
        {
            base.RegisterDependency(specDependency);
        }

        protected override void BuildSpec()
        {
            _specManager.AddFault(FaultSeverity.Debug, $"Any reason to Build a missing Module Spec");
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return base.GetAttributes();
        }

        public override string ToString()
        {
            return $"[M]{ModuleFullName}";
        }

        public override TypeSpec LoadTypeSpec(TypeReference type)
        {
            return _typeSpecs.GetOrAdd(CreateUniqueTypeSpecName(type, type.IsArray), 
                (key) => new MissingTypeSpec($"{type.Namespace}.{type.Name}", type.FullName, _specManager));
        }
    }
}
