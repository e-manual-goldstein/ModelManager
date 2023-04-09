using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface IMethodSpecManager
    {
        IReadOnlyDictionary<MethodDefinition, MethodSpec> Methods { get; }
        MethodSpec LoadMethodSpec(MethodDefinition methodDefinition);

        MethodSpec[] LoadSpecsForMethodReferences(MethodReference[] methodReferences);
        MethodSpec[] TryLoadMethodSpecs(Func<MethodDefinition[]> value);

        void ProcessLoadedMethods(bool includeSystem = true, bool parallelProcessing = true);
        //void ProcessMethods(IEnumerable<MethodSpec> methods, bool includeSystem = true);

    }
}