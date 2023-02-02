using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class FieldSpec : AbstractSpec
    {
        private FieldInfo _fieldInfo;

        public FieldSpec(FieldInfo fieldInfo, ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
        {
            _fieldInfo = fieldInfo;
        }

        public TypeSpec FieldType { get; private set; }


        protected override void BuildSpec()
        {
            //FieldType = _specManager.TryLoadTypeSpec(() => _fieldInfo.FieldType);            
        }

        protected override async Task BeginAnalysis(Analyser analyser)
        {
            Task fieldType = FieldType?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            await Task.WhenAll(fieldType);
        }

        public override string ToString()
        {
            return _fieldInfo.Name;
        }
    }
}
