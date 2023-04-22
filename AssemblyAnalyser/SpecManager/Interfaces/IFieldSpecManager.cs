using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface IFieldSpecManager
    {
        FieldSpec[] FieldSpecs { get; }

        //FieldSpec[] TryLoadFieldSpecs(Func<FieldDefinition[]> value, TypeSpec declaringType);

        //void ProcessLoadedFields(bool includeSystem = true);
    }
}
