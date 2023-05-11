using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface IAttributeSpecManager
    {
        IReadOnlyDictionary<string, TypeSpec> Attributes { get; }

        TypeSpec[] TryLoadAttributeSpecs(Func<CustomAttribute[]> value, AbstractSpec decoratedSpec, ISpecContext specContext);

        void ProcessLoadedAttributes(bool includeSystem = true);
    }
}
