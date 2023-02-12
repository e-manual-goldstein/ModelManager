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
        public MethodSpec Method { get; }
        public bool IsSystemParameter { get; }

        public ParameterSpec(ParameterInfo parameterInfo, MethodSpec method, ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
        {
            _parameterInfo = parameterInfo;
            IsSystemParameter = method.IsSystemMethod;
            Method = method;
        }

        protected override void BuildSpec()
        {
            //Method = _specManager.LoadMethodSpec(_parameterInfo.Member as MethodInfo);            
        }

        protected override async Task BeginAnalysis(Analyser analyser)
        {
            Task parameterType = ParameterType?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            await Task.WhenAll(parameterType);
        }
    }
}
