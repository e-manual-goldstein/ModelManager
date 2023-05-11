using AssemblyAnalyser.Specs;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class ParameterSpec : AbstractSpec
    {
        ParameterDefinition _parameterDefinition;

        public IMemberSpec Member { get; }
        public bool IsOut { get; }
        
        public ParameterSpec(ParameterDefinition parameterDefinition, IMemberSpec member, ISpecManager specManager, ISpecContext specContext)
            : base(specManager, specContext)
        {
            _parameterDefinition = parameterDefinition;
            Name = _parameterDefinition.Name;
            FullName = $"{_parameterDefinition.ParameterType.FullName} {_parameterDefinition.Name}";
            IsSystem = member.IsSystem;
            Member = member;
            IsOut = parameterDefinition.IsOut;
        }

        public string FullName { get; }

        public ParameterDefinition Definition => _parameterDefinition;

        protected override CustomAttribute[] GetAttributes()
        {
            return _parameterDefinition.CustomAttributes.ToArray();
        }

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this, _specContext);
        }

        public bool IsParams => Attributes.Any(a => a.Name == "ParamArrayAttribute");

        TypeSpec _parameterType;
        public TypeSpec ParameterType => _parameterType ??= TryGetParameterType();

        protected override void BuildSpec()
        {
            _parameterType = TryGetParameterType();
        }

        private TypeSpec TryGetParameterType()
        {
            var parameterTypeSpec = _specManager.LoadTypeSpec(_parameterDefinition.ParameterType, _specContext);
            parameterTypeSpec.RegisterAsDependentParameterSpec(this);            
            return parameterTypeSpec;
        }

        public override string ToString()
        {
            return FullName;
        }

        internal bool MatchesParameter(ParameterSpec parameterSpec)
        {
            return ParameterType.MatchesSpec(parameterSpec.ParameterType)
                    && IsOut == parameterSpec.IsOut
                    && IsParams == parameterSpec.IsParams;            
        }
    }
}
