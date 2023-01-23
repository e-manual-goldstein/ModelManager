using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class FieldSpec : ISpec
    {
        private FieldInfo fieldInfo;

        public FieldSpec(FieldInfo fieldInfo)
        {
            this.fieldInfo = fieldInfo;
        }

        public Task AnalyseAsync(Analyser analyser)
        {
            throw new NotImplementedException();
        }
    }
}
