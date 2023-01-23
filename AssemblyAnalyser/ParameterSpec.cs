using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class ParameterSpec : ISpec
    {
        private ParameterInfo _parameterInfo;

        private bool _analysing;

        public TypeSpec ParameterType { get; private set; }

        private bool _analysed;

        public ParameterSpec(ParameterInfo parameterInfo)
        {
            _parameterInfo = parameterInfo;
            InclusionRules = new List<InclusionRule<ParameterSpec>>();
            ExclusionRules = new List<ExclusionRule<ParameterSpec>>();
        }

        public async Task AnalyseAsync(Analyser analyser)
        {
            if (!_analysed && !_analysing)
            {
                _analysing = true;
                ParameterType = analyser.TryLoadTypeSpec(() => _parameterInfo.ParameterType);
                await BeginAnalysis(analyser);
            }
        }

        private async Task BeginAnalysis(Analyser analyser)
        {
            Task parameterType = ParameterType?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            await Task.WhenAll(parameterType);
            _analysed = true;
        }

        public bool Excluded()
        {
            return ExclusionRules.Any(r => r.Exclude(this));
        }

        public bool Included()
        {
            return InclusionRules.All(r => r.Include(this));
        }

        public List<ExclusionRule<ParameterSpec>> ExclusionRules { get; private set; }
        public List<InclusionRule<ParameterSpec>> InclusionRules { get; private set; }
    }
}
