using System.Collections.Generic;
using Mono.Cecil;

namespace AssemblyAnalyser
{
    public interface IModuleSpecManager
    {
        void ProcessAllModules(bool includeSystem = true, bool parallelProcessing = true);

        IReadOnlyDictionary<string, ModuleSpec> Modules { get; }
        ModuleSpec[] LoadModuleSpecs(ModuleDefinition[] modules);
        ModuleSpec LoadModuleSpec(IMetadataScope scope);
        ModuleSpec LoadModuleSpecFromPath(string moduleFilePath);
        ModuleSpec[] LoadReferencedModules(ModuleDefinition module);
        ModuleSpec LoadReferencedModuleByFullName(ModuleDefinition module, string referencedModuleName);
        ModuleSpec LoadReferencedModuleByScopeName(ModuleDefinition module, IMetadataScope scope);
    }
}
