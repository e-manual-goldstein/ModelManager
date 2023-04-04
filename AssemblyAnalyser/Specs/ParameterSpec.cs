﻿using AssemblyAnalyser.Specs;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class ParameterSpec : AbstractSpec
    {
        ParameterDefinition _parameterDefinition;

        public MethodSpec Method { get; }
        public bool IsOut { get; }
        public bool? IsSystemParameter { get; }

        public ParameterSpec(ParameterDefinition parameterDefinition, MethodSpec method, ISpecManager specManager, List<IRule> rules)
            : base(rules, specManager)
        {
            _parameterDefinition = parameterDefinition;
            Name = _parameterDefinition.Name;
            IsSystemParameter = method.IsSystemMethod;
            Method = method;
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
