using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public interface IHasGenericParameters : ISpec
    {
        GenericParameterSpec[] GenericTypeParameters { get; }
    }
}
