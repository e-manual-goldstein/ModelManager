using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AssemblyAnalyser
{
    public interface ITypeSpecManager
    {
        IReadOnlyDictionary<string, TypeSpec> Types { get; }
        bool TryLoadTypeSpec(Func<Type> getType, out TypeSpec typeSpec, AssemblySpec assemblySpec = null);
        bool TryLoadTypeSpec(Func<TypeReference> getType, out TypeSpec typeSpec);
        bool TryLoadTypeSpecs(Func<Type[]> value, out TypeSpec[] typeSpecs, AssemblySpec assemblySpec = null);
        bool TryLoadTypeSpecs(Func<TypeReference[]> value, out TypeSpec[] typeSpecs);
        //TypeSpec[] TryLoadTypesForAssembly(string assemblyFullName);
        TypeSpec[] TryLoadTypesForAssembly(AssemblySpec assemblySpec);
        TypeSpec[] TryLoadTypesForModule(ModuleDefinition module);
        void TryBuildTypeSpecForAssembly(string fullTypeName, string @namespace, string name, AssemblySpec assemblySpec, Action<TypeInfo> buildAction);
        void ProcessAllAssemblies(bool includeSystem = true, bool parallelProcessing = true);
        void ProcessAllLoadedTypes(bool includeSystem = true, bool parallelProcessing = true);
        //void ProcessTypes(IEnumerable<TypeSpec> typeSpecs, bool parallelProcessing = true);
    }
}