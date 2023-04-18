using Mono.Cecil;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface IAssemblySpecManager
    {
        

        AssemblySpec LoadAssemblySpecFromPath(string assemblySpecPath);
        IEnumerable<AssemblySpec> TryLoadReferencedAssemblies(ModuleDefinition moduleDefinition, IAssemblyLocator assemblyLocator);

        IAssemblyResolver AssemblyResolver { get; }

    }
}