﻿using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface IFieldSpecManager
    {
        IReadOnlyDictionary<FieldDefinition, FieldSpec> Fields { get; }

        FieldSpec[] TryLoadFieldSpecs(Func<FieldDefinition[]> value, TypeSpec declaringType);

        void ProcessLoadedFields(bool includeSystem = true);
    }
}
