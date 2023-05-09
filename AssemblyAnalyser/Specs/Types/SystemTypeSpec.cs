using AssemblyAnalyser.Extensions;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class SystemTypeSpec : TypeSpec
    {
        public SystemTypeSpec(TypeDefinition typeDefinition, ModuleSpec moduleSpec, ISpecManager specManager) 
            : base(typeDefinition, moduleSpec, specManager)
        {
        }

        protected SystemTypeSpec(string fullTypeName, string uniqueTypeName, ModuleSpec moduleSpec, ISpecManager specManager) : base(fullTypeName, uniqueTypeName, moduleSpec, specManager)
        {
        }

        public override bool IsSystem => true;

        public override bool IsGenericInstance => base.IsGenericInstance;

        public override bool IsGenericParameter => base.IsGenericParameter;

        public override void AddImplementation(TypeSpec typeSpec)
        {
            base.AddImplementation(typeSpec);
        }

        public override void AddSubType(TypeSpec typeSpec)
        {
            base.AddSubType(typeSpec);
        }

        public override MethodSpec FindMatchingMethodSpec(MethodSpec methodSpec)
        {
            return base.FindMatchingMethodSpec(methodSpec);
        }

        public override MethodSpec[] GetAllMethodSpecs()
        {
            return base.GetAllMethodSpecs();
        }

        public override PropertySpec[] GetAllPropertySpecs()
        {
            return base.GetAllPropertySpecs();
        }

        public override PropertySpec GetPropertySpec(string name, bool includeInherited = false)
        {
            return base.GetPropertySpec(name, includeInherited);
        }

        public override bool MatchesSpec(TypeSpec typeSpec)
        {
            return base.MatchesSpec(typeSpec);
        }

        public override PropertySpec MatchPropertySpecByNameAndParameterType(string name, ParameterSpec[] parameterSpecs, bool includeInherited = false)
        {
            return base.MatchPropertySpecByNameAndParameterType(name, parameterSpecs, includeInherited);
        }

        public override void RegisterAsDecorator(AbstractSpec decoratedSpec)
        {
            base.RegisterAsDecorator(decoratedSpec);
        }

        public override void RegisterAsDelegateFor(EventSpec eventSpec)
        {
            base.RegisterAsDelegateFor(eventSpec);
        }

        public override void RegisterAsDependentParameterSpec(ParameterSpec parameterSpec)
        {
            base.RegisterAsDependentParameterSpec(parameterSpec);
        }

        public override void RegisterAsResultType(IMemberSpec methodSpec)
        {
            base.RegisterAsResultType(methodSpec);
        }

        public override void RegisterDependentMethodSpec(MethodSpec methodSpec)
        {
            base.RegisterDependentMethodSpec(methodSpec);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        protected override void BuildSpec()
        {
            base.BuildSpec();
        }

        protected override void CheckInterfaceImplementations()
        {
            base.CheckInterfaceImplementations();
        }

        protected override TypeSpec[] CreateAttributSpecs()
        {
            return base.CreateAttributSpecs();
        }

        protected override TypeSpec CreateBaseSpec()
        {
            return base.CreateBaseSpec();
        }

        protected override EventSpec[] CreateEventSpecs()
        {
            return base.CreateEventSpecs();
        }

        protected override FieldSpec[] CreateFieldSpecs()
        {
            return base.CreateFieldSpecs();
        }

        protected override GenericParameterSpec[] CreateGenericTypeParameters()
        {
            return base.CreateGenericTypeParameters();
        }

        protected override TypeSpec[] CreateInterfaceSpecs()
        {
            return base.CreateInterfaceSpecs();
        }

        protected override MethodSpec[] CreateMethodSpecs()
        {
            return base.CreateMethodSpecs();
        }

        protected override TypeSpec[] CreateNestedTypeSpecs()
        {
            return base.CreateNestedTypeSpecs();
        }

        protected override PropertySpec[] CreatePropertySpecs()
        {
            return base.CreatePropertySpecs();
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return base.GetAttributes();
        }

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return base.TryLoadAttributeSpecs();
        }

        public override MethodSpec LoadMethodSpec(MethodReference method)
        {
            return base.LoadMethodSpec(method);
        }
    }
}
