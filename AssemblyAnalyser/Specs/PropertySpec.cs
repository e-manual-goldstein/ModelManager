using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class PropertySpec : AbstractSpec, IMemberSpec, IImplementsSpec<PropertySpec>
    {
        private PropertyDefinition _propertyDefinition;
        private MethodDefinition _getter;
        private MethodDefinition _setter;

        public PropertySpec(PropertyDefinition propertyInfo, TypeSpec declaringType, ISpecManager specManager) 
            : base(specManager)
        {
            _propertyDefinition = propertyInfo;
            Name = propertyInfo.Name;
            _getter = propertyInfo.GetMethod;
            _setter = propertyInfo.SetMethod;            
            DeclaringType = declaringType;
            IsSystemProperty = declaringType.IsSystemType;
        }

        public MethodSpec Getter { get; private set; }
        public MethodSpec Setter { get; private set; }

        TypeSpec IMemberSpec.ResultType => PropertyType;
        TypeSpec _propertyType;
        public TypeSpec PropertyType => _propertyType ??= TryGetPropertyType();
                
        public TypeSpec DeclaringType { get; }
        public bool? IsSystemProperty { get; }

        public PropertySpec Implements { get; set; }

        public IEnumerable<MethodDefinition> InnerMethods()
        {
            return new[] { _getter, _setter };
        }

        public IEnumerable<MethodSpec> InnerSpecs()
        {
            return new[] { Getter, Setter }.Where(c => c != null);
        }

        protected override void BuildSpec()
        {
            Getter = _specManager.LoadMethodSpec(_getter, DeclaringType);
            Setter = _specManager.LoadMethodSpec(_setter, DeclaringType);
            _propertyType = TryGetPropertyType();
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        private TypeSpec TryGetPropertyType()
        {
            if (_specManager.TryLoadTypeSpec(() => _propertyDefinition.PropertyType, out TypeSpec typeSpec))
            {
                typeSpec.RegisterAsResultType(this);
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
