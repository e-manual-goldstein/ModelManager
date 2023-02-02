using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface ITypeSpecManager
    {
        IReadOnlyDictionary<string, TypeSpec> Types { get; }
        bool TryLoadTypeSpec(Func<Type> getType, AssemblySpec assemblySpec, out TypeSpec typeSpec);
        bool TryLoadTypeSpecs(Func<Type[]> value, AssemblySpec assemblySpec, out TypeSpec[] typeSpecs);
        //TypeSpec[] TryLoadTypesForAssembly(string assemblyFullName);
        TypeSpec[] TryLoadTypesForAssembly(AssemblySpec assemblySpec);
        void TryBuildTypeSpecForAssembly(string fullTypeName, AssemblySpec assemblySpec, Action<Type> buildAction);
    }
}