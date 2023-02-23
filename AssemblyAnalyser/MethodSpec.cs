using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class MethodSpec : AbstractSpec, IMemberSpec
    {
        MethodInfo _methodInfo;

        public MethodSpec(MethodInfo methodInfo, TypeSpec declaringType, ISpecManager specManager, List<IRule> rules) 
            : base(rules, specManager)
        {
            _methodInfo = methodInfo;
            IsSystemMethod = declaringType.IsSystemType;
            IsConstructor = methodInfo.IsConstructor;
            DeclaringType = declaringType;
        }

        public TypeSpec ResultType { get; private set; }
        public TypeSpec DeclaringType { get; }
        public ParameterSpec[] Parameters { get; private set; }
        List<TypeSpec> _localVariableTypes = new List<TypeSpec>();
        public TypeSpec[] LocalVariableTypes => _localVariableTypes.ToArray();
        List<TypeSpec> _exceptionCatchTypes = new List<TypeSpec>();
        public TypeSpec[] ExceptionCatchTypes => _exceptionCatchTypes.ToArray();
        public bool IsSystemMethod { get; }
        public bool IsConstructor { get; }
        
        protected override void BuildSpec()
        {
            if (_specManager.TryLoadTypeSpec(() => _methodInfo.ReturnType, out TypeSpec returnTypeSpec))
            {
                ResultType = returnTypeSpec;
                returnTypeSpec.RegisterAsResultType(this);
            }
            Parameters = _specManager.TryLoadParameterSpecs(() => _methodInfo.GetParameters(), this);
            if (_methodInfo.GetMethodBody() is MethodBody body)
            {
                ProcessLocalVariables(body);
                ProcessExceptionClauseCatchTypes(body);
            }
            Attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        private CustomAttributeData[] GetAttributes()
        {
            return _methodInfo.GetCustomAttributesData().ToArray();
        }

        private void ProcessExceptionClauseCatchTypes(MethodBody body)
        {
            if (_specManager.TryLoadTypeSpecs(() => body.ExceptionHandlingClauses
                .Where(d => d.Flags == ExceptionHandlingClauseOptions.Clause).Select(d => d.CatchType).ToArray(), 
                out TypeSpec[] exceptionCatchTypes))
            {
                foreach (var catchType in exceptionCatchTypes)
                {
                    if (!_exceptionCatchTypes.Contains(catchType))
                    {
                        _exceptionCatchTypes.Add(catchType);
                        catchType.RegisterDependentMethodSpec(this);
                    }
                }
            }
        }

        private void ProcessLocalVariables(MethodBody body)
        {
            if (_specManager.TryLoadTypeSpecs(() => 
            {
                return body.LocalVariables.Select(d => d.LocalType).ToArray();
            }, out TypeSpec[] localVariableTypes))
            {
                foreach (var localVariableType in localVariableTypes)
                {
                    if (!_localVariableTypes.Contains(localVariableType))
                    {
                        _localVariableTypes.Add(localVariableType);
                        localVariableType.RegisterDependentMethodSpec(this);
                    }
                }
            }
        }

        public override string ToString()
        {
            return _methodInfo.Name;
        }        
    }
}
