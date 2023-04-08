using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class MethodSpec : AbstractSpec, IMemberSpec, IHasParameters, IImplementsSpec<MethodSpec>
    {
        MethodDefinition _methodDefinition;

        public MethodSpec(MethodDefinition methodDefinition, TypeSpec declaringType, ISpecManager specManager)
            : base(specManager)
        {
            _methodDefinition = methodDefinition;
            Name = methodDefinition.Name;
            IsSystem = declaringType.IsSystem;
            IsConstructor = methodDefinition.IsConstructor;
            DeclaringType = declaringType;
        }

        protected MethodSpec(ISpecManager specManager) : base(specManager)
        {

        }

        TypeSpec IMemberSpec.ResultType => ReturnType;

        TypeSpec _returnType;
        public TypeSpec ReturnType => _returnType ??= TryGetReturnType();

        public TypeSpec DeclaringType { get; }

        ParameterSpec[] _parameters;
        public ParameterSpec[] Parameters => _parameters ??= _specManager.TryLoadParameterSpecs(() => _methodDefinition.Parameters.ToArray(), this);

        GenericParameterSpec[] _genericTypeArguments;
        public GenericParameterSpec[] GenericTypeArguments => _genericTypeArguments ??= TryGetGenericTypeArguments();

        List<TypeSpec> _localVariableTypes = new List<TypeSpec>();
        public TypeSpec[] LocalVariableTypes => _localVariableTypes.ToArray();
        List<TypeSpec> _exceptionCatchTypes = new List<TypeSpec>();
        public TypeSpec[] ExceptionCatchTypes => _exceptionCatchTypes.ToArray();

        public bool? IsSystemMethod { get; }
        public bool IsConstructor { get; }
        
        public MethodSpec Implements { get; set; }

        protected override void BuildSpec()
        {
            _returnType = TryGetReturnType();
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

        private GenericParameterSpec[] TryGetGenericTypeArguments()
        {
            if (_specManager.TryLoadTypeSpecs(() => _methodDefinition.GenericParameters.ToArray(), out GenericParameterSpec[] genericArgumentSpecs))
            {
                foreach (var genericArgSpec in genericArgumentSpecs)
                {
                    genericArgSpec.RegisterAsGenericTypeArgumentFor(this);
                }
            }
            return genericArgumentSpecs;
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

        //public bool HasExactParameters(ParameterSpec[] parameterSpecs)
        //{
        //    if (parameterSpecs.Length == Parameters.Length)
        //    {
        //        for (int i = 0; i < parameterSpecs.Length; i++)
        //        {
        //            if (Parameters[i].ParameterType != parameterSpecs[i].ParameterType 
        //                || Parameters[i].IsOut != parameterSpecs[i].IsOut
        //                || Parameters[i].IsParams != parameterSpecs[i].IsParams)
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    }
        //    return false;
        //}

        public bool HasExactGenericTypeArguments(GenericParameterSpec[] genericTypeArgumentSpecs)
        {
            if (genericTypeArgumentSpecs.Length == GenericTypeArguments.Length)
            {
                for (int i = 0; i < GenericTypeArguments.Length; i++)
                {
                    if (!GenericTypeArguments[i].IsValidGenericTypeMatchFor(genericTypeArgumentSpecs[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
