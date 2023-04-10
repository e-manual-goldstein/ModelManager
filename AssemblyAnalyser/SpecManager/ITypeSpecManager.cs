using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface ITypeSpecManager
    {
        
        TypeSpec[] TypeSpecs { get; }
        bool TryLoadTypeSpec(Func<TypeReference> getType, out TypeSpec typeSpec);
        bool TryLoadTypeSpecs(Func<TypeReference[]> value, out TypeSpec[] typeSpecs);
        bool TryLoadTypeSpecs<TSpec>(Func<TypeReference[]> value, out TSpec[] typeSpecs);
        
    }
}