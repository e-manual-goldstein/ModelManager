﻿using System.Collections.Generic;
using Mono.Cecil;

namespace AssemblyAnalyser
{
    public interface IModuleSpecManager
    {
        void ProcessAllModules(bool includeSystem = true, bool parallelProcessing = true);

        IReadOnlyDictionary<string, ModuleSpec> Modules { get; }
        ModuleSpec[] LoadModuleSpecs(ModuleDefinition[] modules);
        ModuleSpec LoadModuleSpec(ModuleDefinition module);
        ModuleSpec[] LoadReferencedModules(ModuleDefinition module);
        ModuleSpec LoadReferencedModule(ModuleDefinition module, string referencedModuleName);
    }
}
