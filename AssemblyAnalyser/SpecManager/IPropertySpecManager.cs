using System;
using System.Collections.Generic;
using System.Reflection;

namespace AssemblyAnalyser
{
    public interface IPropertySpecManager
    {
        IReadOnlyDictionary<PropertyInfo, PropertySpec> Properties { get; }

        PropertySpec[] TryLoadPropertySpecs(Func<PropertyInfo[]> value, TypeSpec declaringType);

    }
}