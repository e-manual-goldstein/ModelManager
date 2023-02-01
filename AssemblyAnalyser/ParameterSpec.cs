using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class ParameterSpec : AbstractSpec
    {
        private ParameterInfo _parameterInfo;

        public TypeSpec ParameterType { get; private set; }
        public MethodSpec Method { get; set; }


        public ParameterSpec(ParameterInfo parameterInfo, List<IRule> rules) : base(rules)
        {
            _parameterInfo = parameterInfo;
        }

        protected override void BeginProcessing(Analyser analyser, ISpecManager specManager)
        {
            ParameterType = specManager.TryLoadTypeSpec(() => _parameterInfo.ParameterType);
            Method = specManager.LoadMethodSpec(_parameterInfo.Member as MethodInfo);            
        }

        protected override async Task BeginAnalysis(Analyser analyser)
        {
            Task parameterType = ParameterType?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            await Task.WhenAll(parameterType);
        }
    }
}
