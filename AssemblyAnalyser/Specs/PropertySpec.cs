using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class PropertySpec : AbstractSpec, IMemberSpec
    {
        private PropertyDefinition _propertyDefinition;
        private MethodDefinition _getter;
        private MethodDefinition _setter;

        public PropertySpec(PropertyDefinition propertyInfo, TypeSpec declaringType, ISpecManager specManager, List<IRule> rules) 
            : base(rules, specManager)
        {
            _propertyDefinition = propertyInfo;
            _getter = propertyInfo.GetMethod;
            _setter = propertyInfo.SetMethod;
            DeclaringType = declaringType;
            IsSystemProperty = declaringType.IsSystemType;
        }

        public MethodSpec Getter { get; private set; }
        public MethodSpec Setter { get; private set; }

        TypeSpec IMemberSpec.ResultType => PropertyType;
        public TypeSpec PropertyType { get; private set; }
                
        public TypeSpec DeclaringType { get; }
        public bool? IsSystemProperty { get; }

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
            if (_specManager.TryLoadTypeSpec(() => _propertyDefinition.PropertyType, out TypeSpec typeSpec))
            {
                PropertyType = typeSpec;
                typeSpec.RegisterAsResultType(this);
                
            }
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
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
