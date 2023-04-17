using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class ArrayTypeSpec : TypeSpec
    {
        ArrayType _arrayType;

        public ArrayTypeSpec(ArrayType arrayType, TypeSpec elementSpec, ISpecManager specManager) :
            base($"{arrayType.ElementType.Namespace}.{arrayType.ElementType.Name}[]", $"{arrayType.ElementType.FullName}[]", specManager)
        {
            _arrayType = arrayType;
            ElementSpec = elementSpec;
        }

        public TypeSpec ElementSpec { get; }

        public override bool IsSystem => ElementSpec.IsSystem;

        public override bool IsInterface => false;

        public override bool IsGenericInstance => false;

        public override bool IsGenericParameter => false;

        public override bool IsNullSpec => false;

        public override void AddImplementation(TypeSpec typeSpec)
        {
            _specManager.AddFault(this, FaultSeverity.Error, "Unexpected Implementation");
        }

        public override void AddSubType(TypeSpec typeSpec)
        {
            _specManager.AddFault(this, FaultSeverity.Error, "Unexpected SubType");
        }

        public override MethodSpec FindMatchingMethodSpec(IHasExplicitName namedMember, MethodSpec methodSpec)
        {
            _specManager.AddFault(this, FaultSeverity.Error, "Unexpected FindMatchingMethodSpec");
            return null;
        }

        public override MethodSpec[] GetAllMethodSpecs()
        {
            _specManager.AddFault(this, FaultSeverity.Error, "Unexpected GetAllMethodSpecs");
            return Array.Empty<MethodSpec>();
        }

        public override PropertySpec[] GetAllPropertySpecs()
        {
            _specManager.AddFault(this, FaultSeverity.Error, "Unexpected GetAllPropertySpecs");            
            return Array.Empty<PropertySpec>();
        }

        public override PropertySpec GetPropertySpec(string name, bool includeInherited = false)
        {
            _specManager.AddFault(this, FaultSeverity.Error, "Unexpected GetAllPropertySpec");
            return null;
        }

        public override bool MatchesSpec(TypeSpec typeSpec)
        {
            return typeSpec is ArrayTypeSpec arrayTypeSpec && arrayTypeSpec.ElementSpec == ElementSpec;
        }

        public override bool MatchMethodByOverride(MethodSpec method)
        {
            _specManager.AddFault(this, FaultSeverity.Error, "Unexpected MatchMethodByOverride");
            return false;
        }

        public override bool MatchPropertyByOverride(PropertySpec property)
        {
            _specManager.AddFault(this, FaultSeverity.Error, "Unexpected MatchMethodByOverride");
            return false;
        }

        public override void RegisterAsDecorator(AbstractSpec decoratedSpec)
        {
            _specManager.AddFault(this, FaultSeverity.Error, "Unexpected RegisterAsDecorator");
        }

        public override void RegisterAsDelegateFor(EventSpec eventSpec)
        {
            _specManager.AddFault(this, FaultSeverity.Error, "Unexpected RegisterAsDelegateFor");
        }

        public override void RegisterAsDependentParameterSpec(ParameterSpec parameterSpec)
        {
            if (!_dependentParameterSpecs.Contains(parameterSpec))
            {
                _dependentParameterSpecs.Add(parameterSpec);                
            }
        }

        public override void RegisterAsResultType(IMemberSpec methodSpec)
        {
            base.RegisterAsResultType(methodSpec);
        }

        public override void RegisterDependentMethodSpec(MethodSpec methodSpec)
        {
            if (!_dependentMethodBodies.Contains(methodSpec))
            {
                _dependentMethodBodies.Add(methodSpec);                
            }
        }

        public override string ToString()
        {
            return FullTypeName;
        }

        protected override void BuildSpec()
        {
            BuildSpecInternal();
        }

        protected override TypeSpec[] CreateAttributSpecs()
        {
            return Array.Empty<TypeSpec>();
        }

        protected override TypeSpec CreateBaseSpec()
        {
            return _specManager.GetNullTypeSpec();
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
            return base.CreateGenericTypeParameters();
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

        protected override bool MatchBySpecialNameMethods(PropertySpec interfaceProperty)
        {
            return base.MatchBySpecialNameMethods(interfaceProperty);
        }

        protected override void ProcessInterfaceImplementations()
        {
            base.ProcessInterfaceImplementations();
        }

        protected override ModuleSpec TryGetModule()
        {
            return _specManager.LoadReferencedModuleByFullName(_arrayType.Module, _arrayType.Scope.Name);
        }
    }
}
