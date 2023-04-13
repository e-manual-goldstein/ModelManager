using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class MethodSpec : AbstractSpec, IMemberSpec, IHasParameters, IHasGenericParameters, IImplementsSpec<MethodSpec>
    {
        MethodDefinition _methodDefinition;

        public MethodSpec(MethodDefinition methodDefinition, ISpecManager specManager)
            : base(specManager)
        {
            _methodDefinition = methodDefinition;
            Name = methodDefinition.Name;
            IsConstructor = methodDefinition.IsConstructor;
            IsSpecialName = methodDefinition.IsSpecialName;
        }

        protected MethodSpec(ISpecManager specManager) : base(specManager)
        {

        }

        public MethodDefinition Definition => _methodDefinition;

        TypeSpec IMemberSpec.ResultType => ReturnType;

        TypeSpec _returnType;
        public TypeSpec ReturnType => _returnType ??= TryGetReturnType();

        TypeSpec _declaringType;
        public TypeSpec DeclaringType => _declaringType ??= TryGetDeclaringType();

        MethodSpec[] _overrides;
        public MethodSpec[] Overrides => _overrides ??= TryGetOverrides();

        ParameterSpec[] _parameters;
        public ParameterSpec[] Parameters => _parameters ??= _specManager.TryLoadParameterSpecs(() => _methodDefinition.Parameters.ToArray(), this);

        GenericParameterSpec[] _genericTypeArguments;
        public GenericParameterSpec[] GenericTypeArguments => _genericTypeArguments ??= TryGetGenericTypeArguments();

        List<TypeSpec> _localVariableTypes = new List<TypeSpec>();
        public TypeSpec[] LocalVariableTypes => _localVariableTypes.ToArray();
        
        List<TypeSpec> _exceptionCatchTypes = new List<TypeSpec>();
        public TypeSpec[] ExceptionCatchTypes => _exceptionCatchTypes.ToArray();

        public override bool IsSystem => DeclaringType.IsSystem;

        public bool IsConstructor { get; }
        public bool IsSpecialName { get; }

        MethodSpec _implements;
        public MethodSpec Implements => _implements;

        public void RegisterAsImplementation(MethodSpec interfaceProperty)
        {
            _implements = interfaceProperty;
        }

        protected override void BuildSpec()
        {
            _returnType = TryGetReturnType();
            _parameters = _specManager.TryLoadParameterSpecs(() => _methodDefinition.Parameters.ToArray(), this);
            if (_methodDefinition.Body is MethodBody body)
            {
                //ProcessMethodBodyOperands(body);
                ProcessLocalVariables(body);
                ProcessExceptionClauseCatchTypes(body);
            }
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        private TypeSpec TryGetReturnType()
        {
            var returnTypeSpec = _specManager.LoadTypeSpec(_methodDefinition.ReturnType);
            returnTypeSpec?.RegisterAsResultType(this);            
            return returnTypeSpec;
        }

        private TypeSpec TryGetDeclaringType()
        {
            return _specManager.LoadTypeSpec(_methodDefinition.DeclaringType);            
        }

        private GenericParameterSpec[] TryGetGenericTypeArguments()
        {
            var genericArgumentSpecs = _specManager
                .LoadTypeSpecs<GenericParameterSpec>(_methodDefinition.GenericParameters).ToArray();
            {
                foreach (var genericArgSpec in genericArgumentSpecs)
                {
                    genericArgSpec.RegisterAsGenericTypeArgumentFor(this);
                }
            }
            return genericArgumentSpecs;
        }

        private MethodSpec[] TryGetOverrides()
        {
            return _specManager.LoadSpecsForMethodReferences(_methodDefinition.Overrides).ToArray();
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
            var exceptionCatchTypes = _specManager.LoadTypeSpecs(body.ExceptionHandlers.Select(d => d.CatchType));
            foreach (var catchType in exceptionCatchTypes.Where(t => !t.IsNullSpec))
            {
                if (!_exceptionCatchTypes.Contains(catchType))
                {
                    _exceptionCatchTypes.Add(catchType);
                    catchType.RegisterDependentMethodSpec(this);
                }
            }
        }        

        private void ProcessLocalVariables(MethodBody body)
        {
            var localVariableTypes =_specManager
                .LoadTypeSpecs(body.Variables.Select(d => d.VariableType)).ToArray();
            
            foreach (var localVariableType in localVariableTypes)
            {
                if (!_localVariableTypes.Contains(localVariableType))
                {
                    _localVariableTypes.Add(localVariableType);
                    localVariableType.RegisterDependentMethodSpec(this);
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
