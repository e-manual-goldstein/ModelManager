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
        public bool? IsSystemParameter { get; }

        public ParameterSpec(ParameterDefinition parameterDefinition, IMemberSpec member, ISpecManager specManager)
            : base(specManager)
        {
            _parameterDefinition = parameterDefinition;
            Name = _parameterDefinition.Name;
            IsSystemParameter = member.IsSystem;
            Member = member;
            IsOut = parameterDefinition.IsOut;
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _parameterDefinition.CustomAttributes.ToArray();
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
            if (_specManager.TryLoadTypeSpec(() => _parameterDefinition.ParameterType, out TypeSpec parameterTypeSpec))
            {
                parameterTypeSpec.RegisterAsDependentParameterSpec(this);
            }
            return parameterTypeSpec;
        }

        public override string ToString()
        {
            return $"{ParameterType.Name} {Name}";
        }
    }
}
