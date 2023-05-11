using AssemblyAnalyser.Extensions;
using Mono.Cecil;
using System;

namespace AssemblyAnalyser.Specs
{
    public class MissingModuleSpec : ModuleSpec
    {
        AssemblyNameReference _missingAssembly;

        public MissingModuleSpec(AssemblyNameReference missingAssembly, AssemblySpec assemblySpec, ISpecManager specManager, ISpecContext specContext) 
            : base(missingAssembly.FullName, assemblySpec, specManager, specContext)
        {
            _missingAssembly = missingAssembly;
            Versions = new();
            ModuleShortName = missingAssembly.Name;
            Versions.Add(missingAssembly.FullName, missingAssembly);
            specManager.AddFault(FaultSeverity.Warning, $"Module not found {missingAssembly.FullName}");
        }

        public AssemblyNameReference MissingAssembly => _missingAssembly;

        public override bool IsSystem { get => base.IsSystem; protected set => base.IsSystem = value; }

        public override void AddChild(ISpecDependency specDependency)
        {
            base.AddChild(specDependency);
        }

        public override void AddParent(ISpecDependency specDependency)
        {
            base.AddParent(specDependency);
        }

        protected override void BuildSpec()
        {
            
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
            return _typeSpecs.GetOrAdd(type.CreateUniqueTypeSpecName(type.IsArray), 
                (key) => new MissingTypeSpec($"{type.Namespace}.{type.Name}", type.FullName, this, _specManager, _specContext));
        }
    }
}
