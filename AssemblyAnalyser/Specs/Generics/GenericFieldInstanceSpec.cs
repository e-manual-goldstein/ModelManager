using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class GenericFieldInstanceSpec : FieldSpec
    {
        private FieldSpec _genericField;

        public GenericFieldInstanceSpec(FieldSpec genericField, GenericInstanceSpec declaringType, ISpecManager specManager, ISpecContext specContext) 
            : base(declaringType, specManager, specContext)
        {
            _genericField = genericField;
        }

        public FieldSpec InstanceOf => _genericField;
    }
}
