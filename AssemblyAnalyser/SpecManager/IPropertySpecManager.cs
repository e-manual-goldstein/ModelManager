using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface IPropertySpecManager
    {
        PropertySpec[] PropertySpecs { get; }

        PropertySpec LoadPropertySpec(PropertyReference property, bool allowNull, IAssemblyLocator assemblyLocator);
        IEnumerable<PropertySpec> LoadPropertySpecs(IEnumerable<PropertyReference> properties, IAssemblyLocator assemblyLocator);        

    }
}