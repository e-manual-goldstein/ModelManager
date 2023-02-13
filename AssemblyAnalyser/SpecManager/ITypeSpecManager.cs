using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface ITypeSpecManager
    {
        IReadOnlyDictionary<string, TypeSpec> Types { get; }
        bool TryLoadTypeSpec(Func<Type> getType, out TypeSpec typeSpec, AssemblySpec assemblySpec = null);
        bool TryLoadTypeSpecs(Func<Type[]> value, out TypeSpec[] typeSpecs, AssemblySpec assemblySpec = null);
        //TypeSpec[] TryLoadTypesForAssembly(string assemblyFullName);
        TypeSpec[] TryLoadTypesForAssembly(AssemblySpec assemblySpec);
        void TryBuildTypeSpecForAssembly(string fullTypeName, AssemblySpec assemblySpec, Action<Type> buildAction);
        void ProcessAllAssemblies(bool includeSystem = true, bool parallelProcessing = true);
        void ProcessAllLoadedTypes(bool includeSystem = true, bool parallelProcessing = true);
        //void ProcessTypes(IEnumerable<TypeSpec> typeSpecs, bool parallelProcessing = true);
    }
}