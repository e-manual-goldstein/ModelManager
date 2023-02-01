using System;
using System.Collections.Generic;
using System.Reflection;

namespace AssemblyAnalyser
{
    public interface ISpecManager : IAssemblySpecManager, ITypeSpecManager
    {

        void SetWorkingDirectory(string workingDirectory);
    }

    public interface ITypeSpecManager
    {
        IReadOnlyDictionary<string, TypeSpec> Types { get; }
        TypeSpec TryLoadTypeSpec(Func<Type> value);
        TypeSpec[] TryLoadTypeSpecs(Func<Type[]> value);
    }

    public interface IAssemblySpecManager
    {
        IReadOnlyDictionary<string, AssemblySpec> Assemblies { get; }
        AssemblySpec[] LoadAssemblySpecs(Assembly[] types);
        AssemblySpec[] LoadAssemblySpecs(AssemblyName[] assemblyNames);
        void LoadAssemblyContext(string filePath, out Assembly assembly);
        AssemblySpec LoadAssemblySpec(Assembly assembly);
    }
}