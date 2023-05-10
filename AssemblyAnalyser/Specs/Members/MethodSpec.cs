using AssemblyAnalyser.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class MethodSpec : AbstractMemberSpec<MethodSpec>, IHasParameters
    {
        protected MethodDefinition _methodDefinition;

        public MethodSpec(MethodDefinition methodDefinition, TypeSpec declaringType, ISpecManager specManager)
            : base(declaringType, specManager)
        {
            _methodDefinition = methodDefinition;
            Name = methodDefinition.Name;
            IsConstructor = methodDefinition.IsConstructor;
            IsSpecialName = methodDefinition.IsSpecialName;
            ExplicitName = methodDefinition.CreateExplicitMemberName();
            IsHideBySig = methodDefinition.IsHideBySig;
            IsOverride = methodDefinition.HasOverrides;
        }

        protected MethodSpec(TypeSpec declaringType, ISpecManager specManager) : base(declaringType, specManager)
        {

        }

        //public string ExplicitName { get; protected set; }

        public MethodDefinition Definition => _methodDefinition;

        public override bool IsSystem => DeclaringType.IsSystem;

        public bool IsConstructor { get; }

        public bool IsSpecialName { get; }

        public override TypeSpec ResultType => ReturnType;

        TypeSpec _returnType;
        public TypeSpec ReturnType => _returnType ??= TryGetReturnType();

        MethodSpec[] _overrides;
        public MethodSpec[] Overrides => _overrides ??= TryGetOverrides();

        ParameterSpec[] _parameters;
        public ParameterSpec[] Parameters => _parameters ??= TryLoadParameterSpecs(() => _methodDefinition.Parameters.ToArray());

        List<TypeSpec> _localVariableTypes = new List<TypeSpec>();
        public TypeSpec[] LocalVariableTypes => _localVariableTypes.ToArray();
        
        List<TypeSpec> _exceptionCatchTypes = new List<TypeSpec>();
        public TypeSpec[] ExceptionCatchTypes => _exceptionCatchTypes.ToArray();

        private void RegisterImplementations()
        {
            foreach (var @override in Overrides)
            {
                if (@override.DeclaringType.IsInterface)
                {
                    RegisterAsImplementation(@override);
                }
                else
                {
                    //Abstract Class Possibly?
                }
            }
            if (!DeclaringType.IsInterface)
            {
                foreach (var @interface in DeclaringType.Interfaces)
                {
                    var method = @interface.FindMatchingMethodSpec(this);
                    if (method != null)
                    {
                        RegisterAsImplementation(method);
                    }
                }
            }
        }

        IMemberSpec _specialNameMethodForMember;
        public IMemberSpec SpecialNameMethodForMember => _specialNameMethodForMember ??= TryGetMemberForSpecialName();

        public void RegisterAsSpecialNameMethodFor(IMemberSpec memberSpec)
        {
            if (_specialNameMethodForMember != null)
            {
                if (_specialNameMethodForMember != memberSpec)
                {
                    _specManager.AddFault(this, FaultSeverity.Error, $"Attempt to over-write {nameof(SpecialNameMethodForMember)}");
                    return;
                }
                _specManager.AddMessage($"Re-registering identical memberSpec for {nameof(SpecialNameMethodForMember)}");               
            }
            _specialNameMethodForMember = memberSpec;
        }

        protected override void BuildSpec()
        {
            _returnType = TryGetReturnType();
            _parameters = TryLoadParameterSpecs(() => _methodDefinition.Parameters.ToArray());
            if (_methodDefinition.Body is MethodBody body)
            {
                //ProcessMethodBodyOperands(body);
                ProcessLocalVariables(body);
                ProcessExceptionClauseCatchTypes(body);
            }
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this, DeclaringType.Module.AssemblyLocator);
            RegisterImplementations();
            base.BuildSpec();
        }

        private TypeSpec TryGetReturnType()
        {
            var returnTypeSpec = _specManager.LoadTypeSpec(_methodDefinition.ReturnType, DeclaringType.Module.AssemblyLocator);
            returnTypeSpec?.RegisterAsResultType(this);            
            return returnTypeSpec;
        }

        protected override TypeSpec TryGetDeclaringType()
        {
            var typeSpec = _specManager.LoadTypeSpec(_methodDefinition.DeclaringType, DeclaringType.Module.AssemblyLocator);
            if (typeSpec.IsNullSpec)
            {
                _specManager.AddFault(this, FaultSeverity.Critical, "Failed to find Declaring Type for spec");
            }
            return typeSpec;
        }

        private MethodSpec[] TryGetOverrides()
        {
            if (_methodDefinition.Overrides.Any())
            {

            }
            return _specManager.LoadSpecsForMethodReferences(_methodDefinition.Overrides, DeclaringType.Module.AssemblyLocator).ToArray();
        }

        private IMemberSpec TryGetMemberForSpecialName()
        {
            var properties = DeclaringType.GetAllPropertySpecs().Where(p => p.GetInnerSpecs().Contains(this)).ToArray();
            if (properties.Count() > 1)
            {
                _specManager.AddFault(this, FaultSeverity.Error, $"Multiple Property Specs found for Special Name");
                return null;
            }
            return properties.SingleOrDefault();
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _methodDefinition.CustomAttributes.ToArray();
        }

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this, DeclaringType.Module.AssemblyLocator);
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
            var exceptionCatchTypes = _specManager.LoadTypeSpecs(body.ExceptionHandlers.Select(d => d.CatchType), DeclaringType.Module.AssemblyLocator);
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
                .LoadTypeSpecs(body.Variables.Select(d => d.VariableType), DeclaringType.Module.AssemblyLocator).ToArray();
            
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
            return ExplicitName;
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

        public virtual bool MatchesSpec(MethodSpec methodSpec)
        {
            return this.HasExactParameters(methodSpec.Parameters);
        }

        protected override MethodSpec TryGetBaseSpec()
        {                    
            if (IsHideBySig)
            {
                return DeclaringType.BaseSpec.FindMatchingMethodSpec(this);
            }
            return null;
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


    }
}
