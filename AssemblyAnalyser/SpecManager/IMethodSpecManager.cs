using System;
using System.Collections.Generic;
using System.Reflection;

namespace AssemblyAnalyser
{
    public interface IMethodSpecManager
    {
        IReadOnlyDictionary<MethodInfo, MethodSpec> Methods { get; }
        MethodSpec LoadMethodSpec(MethodInfo getter);

        MethodSpec[] TryLoadMethodSpecs(Func<MethodInfo[]> value);

        void ProcessLoadedMethods(bool includeSystem = true, bool parallelProcessing = true);
        //void ProcessMethods(IEnumerable<MethodSpec> methods, bool includeSystem = true);

    }
}