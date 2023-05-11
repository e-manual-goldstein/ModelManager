using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface ITypeSpecManager
    {
        
        TypeSpec[] TypeSpecs { get; }
        IEnumerable<TypeSpec> LoadTypeSpecs(IEnumerable<TypeReference> types, ISpecContext specContext);
        IEnumerable<TSpec> LoadTypeSpecs<TSpec>(IEnumerable<TypeReference> types, ISpecContext specContext) where TSpec : TypeSpec;
        TypeSpec LoadTypeSpec(TypeReference type, ISpecContext specContext);
        //bool TryLoadTypeSpec(Func<TypeReference> getType, out TypeSpec typeSpec);
        //bool TryLoadTypeSpecs(Func<TypeReference[]> value, out TypeSpec[] typeSpecs);
        //bool TryLoadTypeSpecs<TSpec>(Func<TypeReference[]> value, out TSpec[] typeSpecs);

    }
}