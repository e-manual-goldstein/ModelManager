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

        public override MethodSpec[] ImplementationFor => base.ImplementationFor;

        public override IMemberSpec[] Implementations => base.Implementations;

        public override bool IsSystem => base.IsSystem;

        public override TypeSpec ResultType => base.ResultType;

        public override bool MatchesSpec(MethodSpec methodSpec)
        {
            return base.MatchesSpec(methodSpec);
        }

        public override void RegisterAsRequiredBy(ISpecDependency specDependency)
        {
            base.RegisterAsRequiredBy(specDependency);
        }

        public override void RegisterDependency(ISpecDependency specDependency)
        {
            base.RegisterDependency(specDependency);
        }

        public override string ToString()
        {
            return method.FullName;
        }

        protected override void BuildSpec()
        {
            base.BuildSpec();
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return base.GetAttributes();
        }

        protected override MethodSpec TryGetBaseSpec()
        {
            return base.TryGetBaseSpec();
        }

        protected override TypeSpec TryGetDeclaringType()
        {
            return base.TryGetDeclaringType();
        }

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return base.TryLoadAttributeSpecs();
        }
    }
}