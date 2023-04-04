using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class FieldSpec : AbstractSpec, IMemberSpec
    {
        const string BACKING_FIELD_SUFFIX = "k__BackingField";
        private FieldDefinition _fieldInfo;

        public FieldSpec(FieldDefinition fieldInfo, TypeSpec declaringType, ISpecManager specManager) 
            : base(specManager)
        {
            _fieldInfo = fieldInfo;
            DeclaringType = declaringType;
            IsSystem = declaringType.IsSystem;
            IsBackingField = fieldInfo.Name.EndsWith(BACKING_FIELD_SUFFIX);
            IsEventField = fieldInfo.DeclaringType.Events.Where(e => e.FullName == fieldInfo.FullName).Count() == 1;
        }

        public string FieldName => _fieldInfo.Name;
        public TypeSpec FieldType { get; private set; }
        
        public bool IsBackingField { get; set; }
        public bool IsEventField { get; set; }
        public TypeSpec DeclaringType { get; }

        TypeSpec IMemberSpec.ResultType => FieldType;

        protected override void BuildSpec()
        {
            if (_specManager.TryLoadTypeSpec(() => _fieldInfo.FieldType, out TypeSpec typeSpec))
            {
                FieldType = typeSpec;
                FieldType.RegisterAsResultType(this);
            }
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _fieldInfo.CustomAttributes.ToArray();
        }

        public override string ToString()
        {
            return _fieldInfo.Name;
        }
    }
}
