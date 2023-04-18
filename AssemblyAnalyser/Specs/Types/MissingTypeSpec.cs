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
        ModuleSpec _moduleSpec;

        public MissingTypeSpec(string fullTypeName, string uniqueTypeName, ModuleSpec moduleSpec, ISpecManager specManager) 
            : base(fullTypeName, uniqueTypeName, moduleSpec, specManager)
        {
            specManager.AddFault(this, FaultSeverity.Error, $"Missing Type Spec for '{fullTypeName}'");            
        }

        public override bool IsMissingSpec => true;

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

        public override bool MatchMethodByOverride(MethodSpec method)
        {
            return false;
        }

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
    }
}
