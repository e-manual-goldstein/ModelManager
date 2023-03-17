using System.Collections.Generic;
using Mono.Cecil;

namespace AssemblyAnalyser
{
    public interface IModuleSpecManager
    {
        IReadOnlyDictionary<string, ModuleSpec> Modules { get; }
        ModuleSpec[] LoadModuleSpecs(ModuleDefinition[] modules);
        ModuleSpec LoadModuleSpec(ModuleDefinition module);
        ModuleSpec[] LoadReferencedModules(ModuleDefinition module);
    }
}
