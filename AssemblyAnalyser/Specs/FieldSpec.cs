﻿using Mono.Cecil;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public class FieldSpec : AbstractSpec, IMemberSpec
    {
        private FieldDefinition _fieldInfo;

        public FieldSpec(FieldDefinition fieldInfo, TypeSpec declaringType, ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
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
