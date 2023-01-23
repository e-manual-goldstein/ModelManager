using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class MethodSpec : ISpec
    {
        MethodInfo _methodInfo;

        private bool _analysing;
        private bool _analysed;

        public MethodSpec(MethodInfo methodInfo)
        {
            _methodInfo = methodInfo;
            InclusionRules = new List<InclusionRule<MethodSpec>>();
            ExclusionRules = new List<ExclusionRule<MethodSpec>>();
        }

        public TypeSpec ReturnType { get; private set; }
        public ParameterSpec[] ParameterTypes { get; private set; }

        public async Task AnalyseAsync(Analyser analyser)
        {
            if (!_analysed && !_analysing)
            {
                _analysing = true;
                ReturnType = analyser.TryLoadTypeSpec(() => _methodInfo.ReturnType);
                ParameterTypes = analyser.TryLoadParameterSpecs(() => _methodInfo.GetParameters());
                await BeginAnalysis(analyser);
            }
        }

        private async Task BeginAnalysis(Analyser analyser)
        {
            Task returnType = ReturnType?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            Task parameterTypes = Task.WhenAll(ParameterTypes?.Select(p => p.AnalyseAsync(analyser)) ?? Enumerable.Empty<Task>());
            await Task.WhenAll(returnType, parameterTypes);
            _analysed = true;
        }

        public override string ToString()
        {
            return _methodInfo.Name;
        }

        public bool Excluded()
        {
            return ExclusionRules.Any(r => r.Exclude(this));
        }

        public bool Included()
        {
            return InclusionRules.All(r => r.Include(this));
        }

        public List<ExclusionRule<MethodSpec>> ExclusionRules { get; private set; }
        public List<InclusionRule<MethodSpec>> InclusionRules { get; private set; }
    }
}
