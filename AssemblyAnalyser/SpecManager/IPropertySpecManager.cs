using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface IPropertySpecManager
    {
        PropertySpec[] PropertySpecs { get; }

        PropertySpec LoadPropertySpec(PropertyReference property);
        IEnumerable<PropertySpec> LoadPropertySpecs(IEnumerable<PropertyReference> properties);        

    }
}