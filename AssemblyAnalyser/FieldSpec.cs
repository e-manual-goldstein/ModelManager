using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class FieldSpec : ISpec
    {
        private FieldInfo _fieldInfo;

        public FieldSpec(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
        }

        public Task AnalyseAsync(Analyser analyser)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return _fieldInfo.Name;
        }
    }
}
