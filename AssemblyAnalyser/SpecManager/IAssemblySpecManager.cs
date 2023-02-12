using System.Collections.Generic;
using System.Reflection;

namespace AssemblyAnalyser
{
    public interface IAssemblySpecManager
    {
        IReadOnlyDictionary<string, AssemblySpec> Assemblies { get; }
        AssemblySpec[] LoadAssemblySpecs(Assembly[] types);
        AssemblySpec LoadAssemblySpec(Assembly assembly);
        AssemblySpec[] LoadReferencedAssemblies(string assemblyFullName, string assemblyFullPath, string targetFrameworkVersion = null, string imageRuntimeVersion = null);
    }
}