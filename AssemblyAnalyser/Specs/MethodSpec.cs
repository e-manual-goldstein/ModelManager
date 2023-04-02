using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class MethodSpec : AbstractSpec, IMemberSpec, IImplementsSpec<MethodSpec>
    {
        MethodDefinition _methodDefinition;

        public MethodSpec(MethodDefinition methodDefinition, TypeSpec declaringType, ISpecManager specManager, List<IRule> rules)
            : base(rules, specManager)
        {
            _methodDefinition = methodDefinition;
            Name = methodDefinition.Name;
            IsSystemMethod = declaringType.IsSystemType;
            IsConstructor = methodDefinition.IsConstructor;
            DeclaringType = declaringType;
        }

        protected MethodSpec(ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
        {

        }

        TypeSpec _resultType;
        public TypeSpec ResultType => _resultType ??= TryGetReturnType();

        public TypeSpec DeclaringType { get; }

        ParameterSpec[] _parameters;
        public ParameterSpec[] Parameters => _parameters ??= _specManager.TryLoadParameterSpecs(() => _methodDefinition.Parameters.ToArray(), this);

        List<TypeSpec> _localVariableTypes = new List<TypeSpec>();
        public TypeSpec[] LocalVariableTypes => _localVariableTypes.ToArray();
        List<TypeSpec> _exceptionCatchTypes = new List<TypeSpec>();
        public TypeSpec[] ExceptionCatchTypes => _exceptionCatchTypes.ToArray();

        public bool? IsSystemMethod { get; }
        public bool IsConstructor { get; }
        
        public MethodSpec Implements { get; set; }

        protected override void BuildSpec()
        {
            _resultType = TryGetReturnType();
            _parameters = _specManager.TryLoadParameterSpecs(() => _methodDefinition.Parameters.ToArray(), this);
            if (_methodDefinition.Body is MethodBody body)
            {
                ProcessMethodBodyOperands(body);
                ProcessLocalVariables(body);
                ProcessExceptionClauseCatchTypes(body);
            }
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        private TypeSpec TryGetReturnType()
        {
            if (_specManager.TryLoadTypeSpec(() => _methodDefinition.ReturnType, out TypeSpec returnTypeSpec))
            {
                returnTypeSpec.RegisterAsResultType(this);
            }
            return returnTypeSpec;
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _methodDefinition.CustomAttributes.ToArray();
        }

        private void ProcessMethodBodyOperands(MethodBody methodBody)
        {
            foreach (var instruction in methodBody.Instructions) 
            {
                if (instruction.Operand != null)
                {
                    var operandType = instruction.Operand.GetType();
                    if (operandType.IsPrimitive || operandType == typeof(string))
                    {
                        continue;
                    }
                    _specManager.RegisterOperandDependency(instruction.Operand, this);
                }
            }
        }

        private void ProcessExceptionClauseCatchTypes(MethodBody body)
        {
            if (_specManager.TryLoadTypeSpecs(() => body.ExceptionHandlers.Select(d => d.CatchType).Where(d => d != null).ToArray(),
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

        public bool IsSpecFor(MethodReference method)
        {
            if (_methodDefinition.Name == method.Name)
            {
                if (method is GenericInstanceMethod != _methodDefinition.IsGenericInstance)
                {
                    return false;
                }
                return !method.Parameters.Any() || MatchParameters(method.Parameters.ToArray());
            }
            return false;
        }

        private bool MatchParameters(ParameterDefinition[] parameters)
        {
            var myParameters = _methodDefinition.Parameters.ToArray();
            if (parameters.Length != myParameters.Length)
            {
                return false;
            }
            for (int i = 0; i < parameters.Length; i++)
            {
                if (myParameters[i].Name != parameters[i].Name || myParameters[i].ParameterType != parameters[i].ParameterType)
                {
                    return false;
                }
            }
            return true;
        }

        public bool HasExactParameterTypes(ParameterSpec[] parameterSpecs)
        {
            for (int i = 0; i < parameterSpecs.Length; i++)
            {
                if (Parameters[i].ParameterType != parameterSpecs[i].ParameterType)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
