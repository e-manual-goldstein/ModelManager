using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AssemblyAnalyser
{
    public class FieldSpec
    {
        FieldInfo _fieldInfo;
        public FieldSpec(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
        }

        public override string ToString()
        {
            return _fieldInfo.Name;
        }
    }
}
