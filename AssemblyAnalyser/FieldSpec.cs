using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class FieldSpec : ISpec
    {
        private FieldInfo _fieldInfo;

        private bool _analysing;
        private bool _analysed;
        private bool _analysable;

        public FieldSpec(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
            InclusionRules = new List<InclusionRule<FieldSpec>>();
            ExclusionRules = new List<ExclusionRule<FieldSpec>>();
        }

        public TypeSpec FieldType { get; private set; }

        public async Task AnalyseAsync(Analyser analyser)
        {
            if (!_analysed && !_analysing)
            {
                _analysing = true;
                FieldType = analyser.TryLoadTypeSpec(() => _fieldInfo.FieldType);
                await BeginAnalysis(analyser);
            }
        }

        private async Task BeginAnalysis(Analyser analyser)
        {
            Task fieldType = FieldType?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            await Task.WhenAll(fieldType);            
            _analysed = true;
        }

        public override string ToString()
        {
            return _fieldInfo.Name;
        }

        public bool Excluded()
        {
            return ExclusionRules.Any(r => r.Exclude(this));
        }

        public bool Included()
        {
            return InclusionRules.All(r => r.Include(this));
        }

        public List<ExclusionRule<FieldSpec>> ExclusionRules { get; private set; }
        public List<InclusionRule<FieldSpec>> InclusionRules { get; private set; }
    }
}
