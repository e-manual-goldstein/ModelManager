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

        public ParameterSpec(ParameterInfo parameterInfo, MethodSpec method, ISpecManager specManager, List<IRule> rules) 
            : base(rules, specManager)
        {
            _parameterInfo = parameterInfo;
            IsSystemParameter = method.IsSystemMethod;
            Method = method;
        }

        protected override void BuildSpec()
        {
            if (_specManager.TryLoadTypeSpec(() => _parameterInfo.ParameterType, out TypeSpec returnTypeSpec))
            {
                ParameterType = returnTypeSpec;
                returnTypeSpec.RegisterAsDependentParameterSpec(this);
            }            
        }
    }
}
