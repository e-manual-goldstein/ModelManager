using AssemblyAnalyser.Specs;
using Mono.Cecil;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    internal class MissingMethodSpec : MethodSpec
    {
        private MethodReference method;

        public MissingMethodSpec(MethodReference method, TypeSpec declaringType, ISpecManager specManager, ISpecContext specContext) 
            : base(declaringType, specManager, specContext)
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

        public override void AddChild(ISpecDependency specDependency)
        {
            base.AddChild(specDependency);
        }

        public override void AddParent(ISpecDependency specDependency)
        {
            base.AddParent(specDependency);
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