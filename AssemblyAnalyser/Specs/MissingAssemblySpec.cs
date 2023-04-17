using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Specs
{
    internal class MissingAssemblySpec : AssemblySpec
    {
        AssemblyNameReference _missingAssembly;

        public MissingAssemblySpec(AssemblyNameReference missingAssembly, ISpecManager specManager)
            : base(missingAssembly.FullName, specManager)
        {
            _missingAssembly = missingAssembly;
            AssemblyShortName = missingAssembly.Name;            
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

        }

        protected override CustomAttribute[] GetAttributes()
        {
            return base.GetAttributes();
        }

        public override string ToString()
        {
            return $"[M]{AssemblyFullName}";
        }

    }
}
