using AssemblyAnalyser.Specs;
using Mono.Cecil;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    internal class MissingMethodSpec : MethodSpec
    {
        private MethodReference method;

        public MissingMethodSpec(MethodReference method, TypeSpec declaringType, ISpecManager specManager) : base(declaringType, specManager)
        {
            this.method = method;
        }

        public override void RegisterAsRequiredBy(ISpecDependency specDependency)
        {
            base.RegisterAsRequiredBy(specDependency);
        }

        public override void RegisterDependency(ISpecDependency specDependency)
        {
            base.RegisterDependency(specDependency);
        }
    }
}