using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class FieldSpec : AbstractMemberSpec<FieldSpec>, IMemberSpec
    {
        const string BACKING_FIELD_SUFFIX = "k__BackingField";
        private FieldDefinition _fieldDefinition;

        public FieldSpec(FieldDefinition fieldInfo, TypeSpec declaringType, ISpecManager specManager, ISpecContext specContext) 
            : this(declaringType, specManager, specContext)
        {
            _fieldDefinition = fieldInfo;
            Name = fieldInfo.Name;
            IsSystem = declaringType.IsSystem;
            IsBackingField = fieldInfo.Name.EndsWith(BACKING_FIELD_SUFFIX);
            IsEventField = fieldInfo.DeclaringType.Events.Where(e => e.FullName == fieldInfo.FullName).Count() == 1;
        }

        protected FieldSpec(TypeSpec declaringType, ISpecManager specManager, ISpecContext specContext) 
            : base(declaringType, specManager, specContext)
        {

        }

        public FieldDefinition Definition => _fieldDefinition;
        
        public string FieldName => _fieldDefinition.Name;

        public TypeSpec FieldType { get; private set; }
        
        public bool IsBackingField { get; set; }
        public bool IsEventField { get; set; }
        
        public override TypeSpec ResultType => FieldType;

        protected override void BuildSpec()
        {
            FieldType = _specManager.LoadTypeSpec(_fieldDefinition.FieldType, _specContext);
            FieldType.RegisterAsResultType(this);            
            _attributes = TryLoadAttributeSpecs();
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _fieldDefinition.CustomAttributes.ToArray();
        }

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this, _specContext);
        }

        public override string ToString()
        {
            return _fieldDefinition.Name;
        }

        protected override TypeSpec TryGetDeclaringType()
        {
            var typeSpec = _specManager.LoadTypeSpec(_fieldDefinition.DeclaringType, _specContext);
            if (typeSpec.IsNullSpec)
            {
                _specManager.AddFault(this, FaultSeverity.Critical, "Failed to find Declaring Type for spec");
            }
            return typeSpec;
        }
    }
}
