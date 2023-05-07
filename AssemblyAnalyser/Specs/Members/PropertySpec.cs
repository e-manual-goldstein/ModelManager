using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class PropertySpec : AbstractMemberSpec<PropertySpec>, IHasParameters
    {
        private PropertyDefinition _propertyDefinition;

        public PropertySpec(PropertyDefinition propertyDefinition, TypeSpec declaringType, ISpecManager specManager)
            : base(declaringType, specManager)
        {
            _propertyDefinition = propertyDefinition;
            Name = propertyDefinition.Name;
            ExplicitName = $"{propertyDefinition.DeclaringType.FullName}.{Name}";
            IsOverride = CheckOverrides();
            IsHideBySig = CheckIsHideBySig();
        }

        public PropertyDefinition Definition => _propertyDefinition;

        private MethodSpec _getter;
        public MethodSpec Getter => _getter ??= TryGetGetter();

        private MethodSpec TryGetGetter()
        {
            var spec = _specManager.LoadMethodSpec(_propertyDefinition.GetMethod, true, DeclaringType.Module.AssemblyLocator);
            spec?.RegisterAsSpecialNameMethodFor(this);
            return spec;
        }

        private MethodSpec _setter;
        public MethodSpec Setter => _setter ??= TryGetSetter();

        private MethodSpec TryGetSetter()
        {
            var spec = _specManager.LoadMethodSpec(_propertyDefinition.SetMethod, true, DeclaringType.Module.AssemblyLocator);
            spec?.RegisterAsSpecialNameMethodFor(this);
            return spec;
        }

        public override TypeSpec ResultType => PropertyType;

        TypeSpec _propertyType;
        public TypeSpec PropertyType => _propertyType ??= GetPropertyType();

        private TypeSpec GetPropertyType()
        {
            var typeSpec = _specManager.LoadTypeSpec(_propertyDefinition.PropertyType, DeclaringType.Module.AssemblyLocator);
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

        PropertySpec[] _overrides;
        public PropertySpec[] Overrides => _overrides ??= TryGetOverrides();

        public override PropertySpec[] ImplementationFor => InnerSpecs().SelectMany(s => s.ImplementationFor)
            .Select(p => p.SpecialNameMethodForMember).OfType<PropertySpec>().ToArray();

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

        private bool CheckOverrides()
        {
            return InnerMethods().Any(m => m?.HasOverrides ?? false);
        }

        private bool CheckIsHideBySig()
        {
            return InnerMethods().Any(m => m?.IsHideBySig ?? false);
        }

        protected override void BuildSpec()
        {
            _getter = TryGetGetter();
            _setter = TryGetSetter();
            _propertyType = GetPropertyType();
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this, DeclaringType.Module.AssemblyLocator);
            base.BuildSpec();
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
            var typeSpec = _specManager.LoadTypeSpec(_propertyDefinition.DeclaringType, DeclaringType.Module.AssemblyLocator);
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

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this, DeclaringType.Module.AssemblyLocator);
        }

        protected override PropertySpec TryGetBaseSpec()
        {
            if (IsHideBySig)
            {
                return DeclaringType.BaseSpec.MatchPropertySpecByNameAndParameterType(Name, Parameters, true);
            }
            return null;
        }

        public override string ToString()
        {
            return $"{ExplicitName}";
        }
    }
}
