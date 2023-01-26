using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class MethodSpec : AbstractSpec
    {
        MethodInfo _methodInfo;

        public MethodSpec(MethodInfo methodInfo, List<IRule> rules) : base(rules)
        {
            _methodInfo = methodInfo;
        }

        public TypeSpec ReturnType { get; private set; }
        public TypeSpec DeclaringType { get; private set; }
        public ParameterSpec[] ParameterTypes { get; private set; }

        protected override void BeginProcessing(Analyser analyser)
        {
            ReturnType = analyser.TryLoadTypeSpec(() => _methodInfo.ReturnType);
            DeclaringType = analyser.TryLoadTypeSpec(() => _methodInfo.DeclaringType);
            ParameterTypes = analyser.TryLoadParameterSpecs(() => _methodInfo.GetParameters());            
        }

        protected override async Task BeginAnalysis(Analyser analyser)
        {
            Task returnType = ReturnType?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            Task parameterTypes = Task.WhenAll(ParameterTypes?.Select(p => p.AnalyseAsync(analyser)) ?? new Task[] { Task.CompletedTask });
            await Task.WhenAll(returnType, parameterTypes);
        }

        public override string ToString()
        {
            return _methodInfo.Name;
        }

        
    }
}
