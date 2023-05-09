using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Specs
{
    internal class MissingTypeSpec : TypeSpec
    {
        public MissingTypeSpec(string fullTypeName, string uniqueTypeName, ModuleSpec moduleSpec, ISpecManager specManager) 
            : base(fullTypeName, uniqueTypeName, moduleSpec, specManager)
        {
            specManager.AddFault(this, FaultSeverity.Error, $"Missing Type Spec for '{fullTypeName}'");            
        }

        public override bool IsMissingSpec => true;

        public override bool IsInterface => false; //Can't say for certain that a Missing Type is an interface

        public override void AddImplementation(TypeSpec typeSpec)
        {
            
        }

        public override void RegisterAsDependentParameterSpec(ParameterSpec parameterSpec)
        {
            
        }

        public override void RegisterAsResultType(IMemberSpec methodSpec)
        {
            
        }

        public override void RegisterAsDecorator(AbstractSpec decoratedSpec)
        {
            
        }

        public override void RegisterAsDelegateFor(EventSpec eventSpec)
        {
            
        }

        public override void RegisterDependentMethodSpec(MethodSpec methodSpec)
        {
            
        }

        public override void AddSubType(TypeSpec typeSpec)
        {
            if (!_subTypes.Contains(typeSpec))
            {
                _subTypes.Add(typeSpec);                
            }
        }

        protected override TypeSpec CreateBaseSpec()
        {
            return null;
        }


        public override PropertySpec[] GetAllPropertySpecs()
        {
            return Array.Empty<PropertySpec>();
        }

        public override MethodSpec[] GetAllMethodSpecs()
        {
            return Array.Empty<MethodSpec>();
        }

        //[Obsolete]
        //public override bool MatchMethodByOverride(MethodSpec method)
        //{
        //    return false;
        //}

        protected override PropertySpec[] CreatePropertySpecs()
        {
            return Array.Empty<PropertySpec>();
        }

        protected override TypeSpec[] CreateInterfaceSpecs()
        {
            return Array.Empty<TypeSpec>();            
        }

        protected override TypeSpec[] CreateNestedTypeSpecs()
        {
            return Array.Empty<TypeSpec>();
        }

        protected override FieldSpec[] CreateFieldSpecs()
        {
            return Array.Empty<FieldSpec>();
        }

        protected override EventSpec[] CreateEventSpecs()
        {
            return Array.Empty<EventSpec>();
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return Array.Empty<CustomAttribute>();
        }

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return base.TryLoadAttributeSpecs();
        }

        protected override MethodSpec[] CreateMethodSpecs()
        {
            return Array.Empty<MethodSpec>();
        }

        public override PropertySpec GetPropertySpec(string name, bool includeInherited = false)
        {
            return base.GetPropertySpec(name, includeInherited);
        }

        protected override void CheckInterfaceImplementations()
        {
            base.CheckInterfaceImplementations();
        }

        //[Obsolete]
        //protected override bool MatchBySpecialNameMethods(PropertySpec interfaceProperty)
        //{
        //    return base.MatchBySpecialNameMethods(interfaceProperty);
        //}

        //[Obsolete]
        //public override bool MatchPropertyByOverride(PropertySpec property)
        //{
        //    return base.MatchPropertyByOverride(property);
        //}

        public override MethodSpec FindMatchingMethodSpec(MethodSpec methodSpec)
        {
            return base.FindMatchingMethodSpec(methodSpec);
        }

        public override MethodSpec LoadMethodSpec(MethodReference method)
        {
            return base.LoadMethodSpec(method);
        }
    }
}
