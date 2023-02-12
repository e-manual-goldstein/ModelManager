using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class MethodSpec : AbstractSpec, IMemberSpec
    {
        MethodInfo _methodInfo;

        public MethodSpec(MethodInfo methodInfo, ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
        {
            _methodInfo = methodInfo;
        }

        public TypeSpec ReturnType { get; private set; }
        public TypeSpec DeclaringType { get; set; }
        public ParameterSpec[] ParameterTypes { get; private set; }

        protected override void BuildSpec()
        {
            if (_specManager.TryLoadTypeSpec(() => _methodInfo.ReturnType, out TypeSpec returnTypeSpec))
            {
                ReturnType = returnTypeSpec;
                returnTypeSpec.RegisterAsReturnType(this);
            }
            ParameterTypes = _specManager.TryLoadParameterSpecs(() => _methodInfo.GetParameters());            
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
