using AssemblyAnalyser.Specs;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class GenericTypeSpec : TypeSpec, IHasGenericParameters
    {


        public GenericTypeSpec(TypeDefinition typeDefinition, ISpecManager specManager) 
            : base(typeDefinition, specManager)
        {
        }

        List<GenericInstanceSpec> _genericInstances = new List<GenericInstanceSpec>();
        public GenericInstanceSpec[] GenericInstances => _genericInstances.ToArray();

        public override bool IsSystem { get => base.IsSystem; protected set => base.IsSystem = value; }

        public override bool IsInterface => base.IsInterface;

        public override bool IsGenericInstance => base.IsGenericInstance;

        public override bool IsGenericParameter => base.IsGenericParameter;

        public override bool IsNullSpec => false;

        public void RegisterAsInstanceOfGenericType(GenericInstanceSpec genericInstanceSpec)
        {
            if (!_genericInstances.Contains(genericInstanceSpec))
            {
                _genericInstances.Add(genericInstanceSpec);
            }
        }

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
            base.BuildSpec();
        }

        protected override TypeSpec[] CreateAttributSpecs()
        {
            return base.CreateAttributSpecs();
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return base.GetAttributes();
        }

        protected override ModuleSpec TryGetModule()
        {
            return base.TryGetModule();
        }

        protected override TypeSpec CreateBaseSpec()
        {
            return base.CreateBaseSpec();
        }

        protected override TypeSpec[] CreateInterfaceSpecs()
        {
            return base.CreateInterfaceSpecs();
        }

        protected override TypeSpec[] CreateNestedTypeSpecs()
        {
            return base.CreateNestedTypeSpecs();
        }

        protected override MethodSpec[] CreateMethodSpecs()
        {
            return base.CreateMethodSpecs();
        }

        public override PropertySpec[] GetAllPropertySpecs()
        {
            return base.GetAllPropertySpecs();
        }

        protected override PropertySpec[] CreatePropertySpecs()
        {
            return base.CreatePropertySpecs();
        }

        protected override FieldSpec[] CreateFieldSpecs()
        {
            return base.CreateFieldSpecs();
        }

        protected override EventSpec[] CreateEventSpecs()
        {
            return base.CreateEventSpecs();
        }

        protected override GenericParameterSpec[] CreateGenericTypeParameters()
        {
            return base.CreateGenericTypeParameters();
        }

        protected override void ProcessInterfaceImplementations()
        {
            base.ProcessInterfaceImplementations();
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

        public override bool MatchesSpec(TypeSpec typeSpec)
        {
            return base.MatchesSpec(typeSpec);
        }

        public override MethodSpec[] GetAllMethodSpecs()
        {
            return base.GetAllMethodSpecs();
        }
    }
}
