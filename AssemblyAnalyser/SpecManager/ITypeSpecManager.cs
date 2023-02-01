using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface ITypeSpecManager
    {
        IReadOnlyDictionary<string, TypeSpec> Types { get; }
        TypeSpec TryLoadTypeSpec(Func<Type> value);
        TypeSpec[] TryLoadTypeSpecs(Func<Type[]> value);
    }
}