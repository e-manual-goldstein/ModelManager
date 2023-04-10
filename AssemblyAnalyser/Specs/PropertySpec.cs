using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class PropertySpec : AbstractSpec, IMemberSpec, IHasParameters, IImplementsSpec<PropertySpec>
    {
        private PropertyDefinition _propertyDefinition;

        public PropertySpec(PropertyDefinition propertyDefinition, ISpecManager specManager) 
            : base(specManager)
        {
            _propertyDefinition = propertyDefinition;
            Name = propertyDefinition.Name;
        }

        private MethodSpec _getter;
        public MethodSpec Getter => _getter ??= TryGetGetter();

        private MethodSpec _setter;
        public MethodSpec Setter => _setter ??= TryGetSetter();

        TypeSpec IMemberSpec.ResultType => PropertyType;
        TypeSpec _propertyType;
        public TypeSpec PropertyType => _propertyType ??= GetPropertyType();

        TypeSpec _declaringType;
        public TypeSpec DeclaringType => _declaringType ??= GetDeclaringType();

        public override bool IsSystem => DeclaringType.IsSystem;

        PropertySpec _implements;
        public PropertySpec Implements => _implements;

        public void RegisterAsImplementation(PropertySpec implementedSpec)
        {
            _implements = implementedSpec;
            _specManager.AddFault(FaultSeverity.Debug, "Is there a scenario where an implemented Property does not match both the underlying Getter and Setter?");
            Getter?.RegisterAsImplementation(implementedSpec.Getter);
            Setter?.RegisterAsImplementation(implementedSpec.Setter);
        }

        ParameterSpec[] _parameters;
        public ParameterSpec[] Parameters => _parameters ??= _specManager.TryLoadParameterSpecs(() => _propertyDefinition.Parameters.ToArray(), this);

        public IEnumerable<MethodDefinition> InnerMethods()
        {
            return new[] { _propertyDefinition.GetMethod, _propertyDefinition.SetMethod };
        }

        public IEnumerable<MethodSpec> InnerSpecs()
        {
            return new[] { Getter, Setter }.Where(c => c != null);
        }

        protected override void BuildSpec()
        {
            _getter = TryGetGetter();
            _setter = TryGetSetter();
            _propertyType = GetPropertyType();
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        private TypeSpec GetPropertyType()
        {
            if (!_specManager.TryLoadTypeSpec(() => _propertyDefinition.PropertyType, out TypeSpec typeSpec))
            {
                _specManager.AddFault(FaultSeverity.Error, $"Could not determine PropertyType for PropertySpec {this}");
            }
            else
            {
                typeSpec.RegisterAsResultType(this);
            }
            return typeSpec;
        }

        private MethodSpec TryGetGetter()
        {
            var spec = _specManager.LoadMethodSpec(_propertyDefinition.GetMethod);
            return spec;
        }

        private MethodSpec TryGetSetter()
        {
            var spec = _specManager.LoadMethodSpec(_propertyDefinition.SetMethod);
            return spec;
        }

        private TypeSpec GetDeclaringType()
        {
            if (!_specManager.TryLoadTypeSpec(() => _propertyDefinition.DeclaringType, out TypeSpec typeSpec))
            {
                _specManager.AddFault(FaultSeverity.Error, $"Could not determine DeclaringType for PropertySpec {this}");
            }
            return typeSpec;
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _propertyDefinition.CustomAttributes.ToArray();
        }

        public override string ToString()
        {
            return _propertyDefinition.Name;
        }
    }
}
