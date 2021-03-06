﻿using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis
{
    public interface IHasDefinedTypes
    {
        List<TypeDefinition> DefinedTypes { get; set; }

		List<TypeDefinition> DefinedTypesIncludingNested();

	}
}
