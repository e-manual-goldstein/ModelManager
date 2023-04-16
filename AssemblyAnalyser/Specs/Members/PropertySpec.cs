using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class PropertySpec : AbstractMemberSpec<PropertySpec>, IHasParameters
    {
        private PropertyDefinition _propertyDefinition;

        public PropertySpec(PropertyDefinition propertyDefinition, ISpecManager specManager)
            : base(specManager)
        {
            _propertyDefinition = propertyDefinition;
            Name = propertyDefinition.Name;
            ExplicitName = $"{propertyDefinition.DeclaringType.FullName}.{Name}";            
        }

        public PropertyDefinition Definition => _propertyDefinition;

        private MethodSpec _getter;
        public MethodSpec Getter => _getter ??= TryGetGetter();

        private MethodSpec _setter;
        public MethodSpec Setter => _setter ??= TryGetSetter();

        public override TypeSpec ResultType => PropertyType;

        TypeSpec _propertyType;
        public TypeSpec PropertyType => _propertyType ??= GetPropertyType();

        PropertySpec[] _overrides;
        public PropertySpec[] Overrides => _overrides ??= TryGetOverrides();

        public override bool IsSystem => DeclaringType.IsSystem;

        ParameterSpec[] _parameters;
        public ParameterSpec[] Parameters => _parameters ??= TryLoadParameterSpecs(() => _propertyDefinition.Parameters.ToArray());

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
            var typeSpec = _specManager.LoadTypeSpec(_propertyDefinition.PropertyType);
            if (typeSpec == null || typeSpec.IsNullSpec)
            {
                _specManager.AddFault(this, FaultSeverity.Error, $"Could not determine PropertyType for PropertySpec {this}");
            }
            else
            {
                typeSpec.RegisterAsResultType(this);
            }
            return typeSpec;
        }

        private MethodSpec TryGetGetter()
        {
            var spec = _specManager.LoadMethodSpec(_propertyDefinition.GetMethod, true);
            spec?.RegisterAsSpecialNameMethodFor(this);
            return spec;
        }

        private MethodSpec TryGetSetter()
        {
            var spec = _specManager.LoadMethodSpec(_propertyDefinition.SetMethod, true);
            spec?.RegisterAsSpecialNameMethodFor(this);
            return spec;
        }

        private PropertySpec[] TryGetOverrides()
        {
            var getterOverrides = TryGetGetterOverrides();
            var setterOverrides = TryGetSetterOverrides();
            if ((getterOverrides.Except(setterOverrides).Any() && Setter != null) 
                || (setterOverrides.Except(getterOverrides).Any() && Getter != null))
            {
                _specManager.AddFault(FaultSeverity.Critical, "Unexpected mismatch of Overrides");
                return Array.Empty<PropertySpec>();
            }
            return getterOverrides.Intersect(setterOverrides).Cast<PropertySpec>().ToArray();
        }

        private PropertySpec[] TryGetGetterOverrides()
        {
            return Getter?.Overrides.Select(o => o.SpecialNameMethodForMember).Cast<PropertySpec>().ToArray() ?? Array.Empty<PropertySpec>();            
        }

        private PropertySpec[] TryGetSetterOverrides()
        {
            return Setter?.Overrides.Select(o => o.SpecialNameMethodForMember).Cast<PropertySpec>().ToArray() ?? Array.Empty<PropertySpec>();
        }

        protected override TypeSpec TryGetDeclaringType()
        {
            var typeSpec = _specManager.LoadTypeSpec(_propertyDefinition.DeclaringType);
            if (typeSpec == null || typeSpec.IsNullSpec)
            {
                _specManager.AddFault(this, FaultSeverity.Critical, $"Could not determine DeclaringType for PropertySpec {this}");
            }
            return typeSpec;
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _propertyDefinition.CustomAttributes.ToArray();
        }

        public override string ToString()
        {
            return $"{ExplicitName}";
        }
    }
}
