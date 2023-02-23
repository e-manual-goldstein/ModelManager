using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class FieldSpec : AbstractSpec, IMemberSpec
    {
        private FieldInfo _fieldInfo;

        public FieldSpec(FieldInfo fieldInfo, TypeSpec declaringType, ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
        {
            _fieldInfo = fieldInfo;
            DeclaringType = declaringType;
            IsSystemField = declaringType.IsSystemType;
        }

        public string FieldName => _fieldInfo.Name;
        public TypeSpec FieldType { get; private set; }
        public bool IsSystemField { get; set; }
        public TypeSpec DeclaringType { get; }

        TypeSpec IMemberSpec.ResultType => FieldType;

        protected override void BuildSpec()
        {
            if (_specManager.TryLoadTypeSpec(() => _fieldInfo.FieldType, out TypeSpec typeSpec))
            {
                FieldType = typeSpec;
                FieldType.RegisterAsResultType(this);
            }
            Attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        private CustomAttributeData[] GetAttributes()
        {
            return _fieldInfo.GetCustomAttributesData().ToArray();
        }

        public override string ToString()
        {
            return _fieldInfo.Name;
        }
    }
}
