using System.Collections.Generic;
using System.Reflection;

namespace AssemblyAnalyser
{
    public interface IAssemblySpecManager
    {
        IReadOnlyDictionary<string, AssemblySpec> Assemblies { get; }
        AssemblySpec[] LoadAssemblySpecs(Assembly[] types);
        AssemblySpec[] LoadAssemblySpecs(AssemblyName[] assemblyNames);
        AssemblySpec LoadAssemblySpec(Assembly assembly);
        AssemblySpec[] LoadReferencedAssemblies(string assemblyFullName);
    }
}