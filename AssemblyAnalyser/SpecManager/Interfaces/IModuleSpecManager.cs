using System.Collections.Generic;
using Mono.Cecil;

namespace AssemblyAnalyser
{
    public interface IModuleSpecManager
    {
        //void ProcessAllModules(bool includeSystem = true, bool parallelProcessing = true);

        ModuleSpec[] ModuleSpecs { get; }
        //ModuleSpec[] LoadModuleSpecs(ModuleDefinition[] modules);
        //ModuleSpec LoadModuleSpec(IMetadataScope scope);
        //ModuleSpec LoadModuleSpecFromPath(string moduleFilePath);
        IEnumerable<ModuleSpec> LoadReferencedModules(ModuleDefinition module, ISpecContext specContext);
        //ModuleSpec LoadReferencedModuleByFullName(ModuleDefinition module, AssemblyNameReference assemblyNameReference, 
        //    IAssemblyLocator assemblyLocator = null);
        //ModuleSpec LoadReferencedModuleByScopeName(ModuleDefinition module, IMetadataScope scope);
    }
}
