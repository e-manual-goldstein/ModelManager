using System;
using System.Collections.Generic;
using System.Reflection;

namespace AssemblyAnalyser
{
    public interface IParameterSpecManager
    {
        IReadOnlyDictionary<ParameterInfo, ParameterSpec> Parameters { get; }
        ParameterSpec[] TryLoadParameterSpecs(Func<ParameterInfo[]> value);

    }
}