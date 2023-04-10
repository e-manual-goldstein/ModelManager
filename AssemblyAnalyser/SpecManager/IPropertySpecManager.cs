using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface IPropertySpecManager
    {
        IReadOnlyDictionary<PropertyDefinition, PropertySpec> Properties { get; }

        PropertySpec[] TryLoadPropertySpecs(Func<PropertyDefinition[]> value);
        void ProcessLoadedProperties(bool includeSystem = true);
        

    }
}