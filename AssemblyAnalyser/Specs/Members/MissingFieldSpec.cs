using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    internal class MissingFieldSpec : FieldSpec
    {
        FieldReference _fieldReference;

        public MissingFieldSpec(FieldReference field, TypeSpec declaringType, ISpecManager specManager, ISpecContext specContext)
            : base(declaringType, specManager, specContext)
        {
            _fieldReference = field;
        }
    }
}
