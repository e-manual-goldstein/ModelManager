using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AssemblyAnalyser
{
    public interface IParameterSpecManager
    {
        IReadOnlyDictionary<ParameterDefinition, ParameterSpec> Parameters { get; }
        //ParameterSpec[] TryLoadParameterSpecs(Func<ParameterInfo[]> value, MethodSpec method);
        ParameterSpec[] TryLoadParameterSpecs(Func<ParameterDefinition[]> value, MethodSpec method);
        void ProcessLoadedParameters(bool includeSystem = true);
    }
}