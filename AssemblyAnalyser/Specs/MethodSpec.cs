using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class MethodSpec : AbstractSpec, IMemberSpec
    {
        MethodDefinition _methodDefinition;

        public MethodSpec(MethodDefinition methodDefinition, TypeSpec declaringType, ISpecManager specManager, List<IRule> rules)
            : base(rules, specManager)
        {
            _methodDefinition = methodDefinition;
            IsSystemMethod = declaringType.IsSystemType;
            IsConstructor = methodDefinition.IsConstructor;
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
            if (_specManager.TryLoadTypeSpec(() => _methodDefinition.ReturnType, out TypeSpec returnTypeSpec))
            {
                ResultType = returnTypeSpec;
                returnTypeSpec.RegisterAsResultType(this);
            }
            Parameters = _specManager.TryLoadParameterSpecs(() => _methodDefinition.Parameters.ToArray(), this);
            if (_methodDefinition.Body is MethodBody body)
            {
                ProcessLocalVariables(body);
                ProcessExceptionClauseCatchTypes(body);
            }
            Attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        private CustomAttribute[] GetAttributes()
        {
            return _methodDefinition.CustomAttributes.ToArray();
        }

        private void ProcessExceptionClauseCatchTypes(MethodBody body)
        {
            //if (_specManager.TryLoadTypeSpecs(() => body.ExceptionHandlers
            //    .Where(d => d.Flags == ExceptionHandlingClauseOptions.Clause).Select(d => d.CatchType).ToArray(), 
            //    out TypeSpec[] exceptionCatchTypes))
            //{
            //    foreach (var catchType in exceptionCatchTypes)
            //    {
            //        if (!_exceptionCatchTypes.Contains(catchType))
            //        {
            //            _exceptionCatchTypes.Add(catchType);
            //            catchType.RegisterDependentMethodSpec(this);
            //        }
            //    }
            //}
        }

        private void ProcessLocalVariables(MethodBody body)
        {
            if (_specManager.TryLoadTypeSpecs(() => 
            {
                return body.Variables.Select(d => d.VariableType).ToArray();
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
            return _methodDefinition.Name;
        }        
    }
}
