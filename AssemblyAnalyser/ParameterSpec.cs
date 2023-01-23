using System;
using System.Collections.Generic;
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
        }

        public async Task AnalyseAsync(Analyser analyser)
        {
            if (!_analysed && !_analysing)
            {
                _analysing = true;
                ParameterType = analyser.LoadTypeSpec(_parameterInfo.ParameterType);
                await BeginAnalysis(analyser);
            }
        }

        private async Task BeginAnalysis(Analyser analyser)
        {
            Task parameterType = ParameterType.AnalyseAsync(analyser);
            await Task.WhenAll(parameterType);
            _analysed = true;
        }
    }
}
