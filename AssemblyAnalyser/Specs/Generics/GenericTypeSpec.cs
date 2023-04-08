using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class GenericTypeSpec : TypeSpec
    {


        public GenericTypeSpec(TypeDefinition typeDefinition, ISpecManager specManager) 
            : base(typeDefinition, specManager)
        {
        }

        List<GenericInstanceSpec> _genericInstances = new List<GenericInstanceSpec>();
        public GenericInstanceSpec[] GenericInstances => _genericInstances.ToArray();

        public void RegisterAsInstanceOfGenericType(GenericInstanceSpec genericInstanceSpec)
        {
            if (!_genericInstances.Contains(genericInstanceSpec))
            {
                _genericInstances.Add(genericInstanceSpec);
            }
        }
    }
}
