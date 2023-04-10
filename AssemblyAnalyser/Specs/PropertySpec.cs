using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class PropertySpec : AbstractSpec, IMemberSpec, IHasParameters, IImplementsSpec<PropertySpec>
    {
        private PropertyDefinition _propertyDefinition;
        private MethodDefinition _getter;
        private MethodDefinition _setter;

        public PropertySpec(PropertyDefinition propertyDefinition, ISpecManager specManager) 
            : base(specManager)
        {
            _propertyDefinition = propertyDefinition;
            Name = propertyDefinition.Name;
            _getter = propertyDefinition.GetMethod;
            _setter = propertyDefinition.SetMethod;
        }

        public MethodSpec Getter { get; private set; }
        public MethodSpec Setter { get; private set; }

        TypeSpec IMemberSpec.ResultType => PropertyType;
        TypeSpec _propertyType;
        public TypeSpec PropertyType => _propertyType ??= GetPropertyType();

        TypeSpec _declaringType;
        public TypeSpec DeclaringType => _declaringType ??= GetDeclaringType();

        public override bool IsSystem => DeclaringType.IsSystem;

        public PropertySpec Implements { get; set; }

        ParameterSpec[] _parameters;
        public ParameterSpec[] Parameters => _parameters ??= _specManager.TryLoadParameterSpecs(() => _propertyDefinition.Parameters.ToArray(), this);

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
            Getter = _specManager.LoadMethodSpec(_getter);
            Setter = _specManager.LoadMethodSpec(_setter);
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
