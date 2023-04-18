using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface IMethodSpecManager
    {
        MethodSpec[] MethodSpecs { get; }
        MethodSpec LoadMethodSpec(MethodDefinition methodDefinition, bool allowNull, IAssemblyLocator assemblyLocator);

        IEnumerable<MethodSpec> LoadSpecsForMethodReferences(IEnumerable<MethodReference> methodReferences, IAssemblyLocator assemblyLocator);        

    }
}