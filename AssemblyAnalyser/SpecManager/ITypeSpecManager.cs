using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface ITypeSpecManager
    {
        IReadOnlyDictionary<string, TypeSpec> Types { get; }
        bool TryLoadTypeSpec(Func<TypeReference> getType, out TypeSpec typeSpec);
        bool TryLoadTypeSpecs(Func<TypeReference[]> value, out TypeSpec[] typeSpecs);
        TypeSpec[] TryLoadTypesForModule(ModuleDefinition module);
        void ProcessAllLoadedTypes(bool includeSystem = true, bool parallelProcessing = true);        
    }
}