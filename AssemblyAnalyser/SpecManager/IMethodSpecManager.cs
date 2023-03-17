using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface IMethodSpecManager
    {
        IReadOnlyDictionary<MethodDefinition, MethodSpec> Methods { get; }
        MethodSpec LoadMethodSpec(MethodDefinition getter, TypeSpec declaringType);

        //MethodSpec[] TryLoadMethodSpecs(Func<MethodInfo[]> value, TypeSpec declaringType);
        MethodSpec[] TryLoadMethodSpecs(Func<MethodDefinition[]> value, TypeSpec declaringType);

        void ProcessLoadedMethods(bool includeSystem = true, bool parallelProcessing = true);
        //void ProcessMethods(IEnumerable<MethodSpec> methods, bool includeSystem = true);

    }
}