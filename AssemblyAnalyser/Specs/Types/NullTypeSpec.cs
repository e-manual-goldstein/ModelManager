using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Specs
{
    public class NullTypeSpec : TypeSpec
    {
        public NullTypeSpec(ISpecManager specManager)
            : base("null", "nullspec", null, specManager)
        {
            Exclude("Null Spec");
            SkipProcessing("Null Spec");
        }

        public override bool IsSystem => true;

        public override bool IsInterface => false;

        public override bool IsGenericInstance => false;

        public override bool IsGenericParameter => false;

        public override bool IsNullSpec => true;

        public override void AddImplementation(TypeSpec typeSpec)
        {
            _specManager.AddFault(this, FaultSeverity.Error, $"{typeSpec} cannot implement Null Type Spec");
        }

        public override PropertySpec[] GetAllPropertySpecs()
        {
            return Array.Empty<PropertySpec>();
        }

        public override PropertySpec GetPropertySpec(string name, bool includeInherited = false)
        {
            return null;
        }

        public override MethodSpec[] GetAllMethodSpecs()
        {
            return Array.Empty<MethodSpec>();
        }

        public override bool MatchesSpec(TypeSpec typeSpec)
        {
            return typeSpec.IsNullSpec;
        }

        public override void RegisterAsDecorator(AbstractSpec decoratedSpec)
        {
            _specManager.AddFault(this, FaultSeverity.Error, $"Null Type Spec cannot decorate {decoratedSpec}");
        }

        public override void RegisterAsDelegateFor(EventSpec eventSpec)
        {
            _specManager.AddFault(this, FaultSeverity.Error, $"Null Type Spec cannot act as delegate for {eventSpec}");
        }

        public override void RegisterAsDependentParameterSpec(ParameterSpec parameterSpec)
        {
            _specManager.AddFault(this, FaultSeverity.Error, $"{parameterSpec} cannot use Null Type Spec as Parameter Type");
        }

        public override void RegisterAsRequiredBy(ISpecDependency specDependency)
        {
            _specManager.AddFault(this, FaultSeverity.Warning, $"Can {specDependency} 'Require' the Null Type Spec?");
            _specManager.AddFault(this, FaultSeverity.Error, $"See Warning");
        }

        public override void RegisterAsResultType(IMemberSpec memberSpec)
        {
            _specManager.AddFault(this, FaultSeverity.Error, $"Null Type Spec cannot be a Result Type for {memberSpec}");
        }

        public override void RegisterDependency(ISpecDependency specDependency)
        {
            _specManager.AddFault(this, FaultSeverity.Warning, $"Can Null Type Spec 'Require' {specDependency}?");
            _specManager.AddFault(this, FaultSeverity.Error, $"See Warning");
        }

        public override void RegisterDependentMethodSpec(MethodSpec methodSpec)
        {
            _specManager.AddFault(FaultSeverity.Warning, $"Can {methodSpec} depend on Null Type Spec?");
            base.RegisterDependentMethodSpec(methodSpec);
        }

        public override string ToString()
        {
            return "Null Type Spec";
        }

        protected override void BuildSpec()
        {
            //base.BuildSpec();
        }

        protected override TypeSpec[] CreateAttributSpecs()
        {
            return Array.Empty<TypeSpec>();
        }

        protected override TypeSpec CreateBaseSpec()
        {
            return this;//Is Null Type Spec's base type also Null Type Spec
        }

        protected override EventSpec[] CreateEventSpecs()
        {
            return Array.Empty<EventSpec>();
        }

        protected override FieldSpec[] CreateFieldSpecs()
        {
            return Array.Empty<FieldSpec>();
        }

        protected override GenericParameterSpec[] CreateGenericTypeParameters()
        {
            return Array.Empty<GenericParameterSpec>();
        }

        protected override TypeSpec[] CreateInterfaceSpecs()
        {
            return Array.Empty<TypeSpec>();
        }

        protected override MethodSpec[] CreateMethodSpecs()
        {
            return Array.Empty<MethodSpec>();
        }

        protected override TypeSpec[] CreateNestedTypeSpecs()
        {
            return Array.Empty<TypeSpec>();
        }

        protected override PropertySpec[] CreatePropertySpecs()
        {
            return Array.Empty<PropertySpec>();
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return Array.Empty<CustomAttribute>();
        }

        protected override void CheckInterfaceImplementations()
        {

        }

        //public override MethodSpec MatchMethodSpecByNameAndParameterType(string methodName, ParameterSpec[] parameterSpecs, 
        //    GenericParameterSpec[] genericTypeArgumentSpecs)
        //{
        //    return null;
        //}

        public override MethodSpec FindMatchingMethodSpec(IHasExplicitName namedMember, MethodSpec methodSpec)
        {
            return null;
        }

        [Obsolete]
        public override bool MatchMethodByOverride(MethodSpec method)
        {
            return false;
        }

        [Obsolete]
        public override bool MatchPropertyByOverride(PropertySpec property)
        {
            return false;
        }

        [Obsolete]
        protected override bool MatchBySpecialNameMethods(PropertySpec interfaceProperty)
        {
            _specManager.AddFault(interfaceProperty, FaultSeverity.Warning, "Backed Property not found");
            return false;
        }
    }
}
