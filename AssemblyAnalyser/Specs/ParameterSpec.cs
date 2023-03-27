﻿using AssemblyAnalyser.Specs;
using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public class ParameterSpec : AbstractSpec
    {
        ParameterDefinition _parameterDefinition;

        public TypeSpec ParameterType { get; private set; }
        public MethodSpec Method { get; }
        public bool? IsSystemParameter { get; }

        public ParameterSpec(ParameterDefinition parameterDefinition, MethodSpec method, ISpecManager specManager, List<IRule> rules)
            : base(rules, specManager)
        {
            _parameterDefinition = parameterDefinition;
            IsSystemParameter = method.IsSystemMethod;
            Method = method;
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _parameterDefinition.CustomAttributes.ToArray();
        }

        protected override void BuildSpec()
        {
            if (_specManager.TryLoadTypeSpec(() => _parameterDefinition.ParameterType, out TypeSpec returnTypeSpec))
            {
                ParameterType = returnTypeSpec;
                returnTypeSpec.RegisterAsDependentParameterSpec(this);
            }            
        }

    }
}
