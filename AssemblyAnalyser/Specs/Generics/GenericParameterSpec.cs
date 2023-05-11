using AssemblyAnalyser.Extensions;
using AssemblyAnalyser.Specs;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class GenericParameterSpec : TypeSpec
    {
        GenericParameter _genericParameter;

        public GenericParameterSpec(GenericParameter genericParameter, ModuleSpec moduleSpec, ISpecManager specManager, ISpecContext specContext) 
            : base($"{genericParameter.Namespace}.{genericParameter.Name}", genericParameter.FullName, moduleSpec, specManager, specContext)
        {
            _genericParameter = genericParameter;
            Name = _genericParameter.Name;
            HasDefaultConstructorConstraint = _genericParameter.Attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint);
        }

        public GenericParameter GenericParameter => _genericParameter;

        public bool HasDefaultConstructorConstraint { get; }

        protected override void BuildSpec()
        {
            BuildSpecInternal();
        }

        protected override TypeSpec CreateBaseSpec()
        {
            foreach (var constraint in _genericParameter.Constraints)
            {
                var typeSpec = _specManager.LoadTypeSpec(constraint.ConstraintType, _specContext);
                if (typeSpec != null && typeSpec.IsClass)
                {
                    return typeSpec;
                }                
            }
            return null;
        }

        public override bool IsGenericParameter => true;

        IHasGenericParameters _genericParameterFor;
        public IHasGenericParameters GenericParameterFor => _genericParameterFor;

        public override bool IsSystem => GenericParameterFor.IsSystem;

        public void RegisterAsGenericTypeParameterFor(GenericMethodSpec genericMethodSpec)
        {
            _genericParameterFor = genericMethodSpec;
            _specManager.AddMessage("Implementation not finished for 'RegisterAsGenericTypeParameterFor'");
        }

        internal bool IsValidGenericTypeMatchFor(GenericParameterSpec genericTypeArgumentSpec)
        {
            _specManager.AddFault(FaultSeverity.Debug, "Implementation not finished for 'IsValidGenericTypeMatchFor'");
            return BaseSpec == genericTypeArgumentSpec.BaseSpec
                && HasDefaultConstructorConstraint == genericTypeArgumentSpec.HasDefaultConstructorConstraint;
        }

        public override bool MatchesSpec(TypeSpec typeSpec)
        {
            return (typeSpec is GenericParameterSpec genericParameterSpec) ? IsValidGenericTypeMatchFor(genericParameterSpec) : false;
        }

        public override void AddChild(ISpecDependency specDependency)
        {
            base.AddChild(specDependency);
        }

        public override void AddParent(ISpecDependency specDependency)
        {
            base.AddParent(specDependency);
        }

        protected override TypeSpec[] CreateAttributSpecs()
        {
            return Array.Empty<TypeSpec>();
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return base.GetAttributes();
        }

        protected override TypeSpec[] CreateInterfaceSpecs()
        {
            _specManager.AddFault(FaultSeverity.Debug, "Unfinished CreateInterfaceSpecs for GenericParameterSpec");
            return Array.Empty<TypeSpec>();
        }

        protected override TypeSpec[] CreateNestedTypeSpecs()
        {
            return Array.Empty<TypeSpec>();
        }

        protected override MethodSpec[] CreateMethodSpecs()
        {
            _specManager.AddFault(FaultSeverity.Debug, "Unfinished CreateMethodSpecs for GenericParameterSpec");
            return Array.Empty<MethodSpec>();
        }

        public override PropertySpec[] GetAllPropertySpecs()
        {
            return base.GetAllPropertySpecs();
        }

        protected override PropertySpec[] CreatePropertySpecs()
        {
            _specManager.AddFault(FaultSeverity.Debug, "Unfinished CreatePropertySpecs for GenericParameterSpec");
            return Array.Empty<PropertySpec>();
        }

        protected override FieldSpec[] CreateFieldSpecs()
        {
            _specManager.AddFault(FaultSeverity.Debug, "Unfinished CreateFieldSpecs for GenericParameterSpec");
            return Array.Empty<FieldSpec>();
        }

        protected override EventSpec[] CreateEventSpecs()
        {
            _specManager.AddFault(FaultSeverity.Debug, "Unfinished CreateEventSpecs for GenericParameterSpec");
            return Array.Empty<EventSpec>();
        }

        protected override GenericParameterSpec[] CreateGenericTypeParameters()
        {
            return base.CreateGenericTypeParameters();
        }

        protected override void CheckInterfaceImplementations()
        {
            _specManager.AddFault(FaultSeverity.Debug, "Unfinished ProcessInterfaceImplementations for GenericParameterSpec");            
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override void AddImplementation(TypeSpec typeSpec)
        {
            base.AddImplementation(typeSpec);
        }

        public override void RegisterAsResultType(IMemberSpec methodSpec)
        {
            base.RegisterAsResultType(methodSpec);
        }

        public override void RegisterAsDependentParameterSpec(ParameterSpec parameterSpec)
        {
            base.RegisterAsDependentParameterSpec(parameterSpec);
        }

        public override void RegisterDependentMethodSpec(MethodSpec methodSpec)
        {
            base.RegisterDependentMethodSpec(methodSpec);
        }

        public override void RegisterAsDecorator(AbstractSpec decoratedSpec)
        {
            base.RegisterAsDecorator(decoratedSpec);
        }

        public override void RegisterAsDelegateFor(EventSpec eventSpec)
        {
            base.RegisterAsDelegateFor(eventSpec);
        }

        public override MethodSpec[] GetAllMethodSpecs()
        {
            return base.GetAllMethodSpecs();
        }

        public override MethodSpec LoadMethodSpec(MethodReference method)
        {
            return base.LoadMethodSpec(method);
        }
    }
}
