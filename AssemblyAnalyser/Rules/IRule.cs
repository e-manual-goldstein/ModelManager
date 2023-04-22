using System;
using System.Collections.Generic;
using System.Text;

namespace AssemblyAnalyser
{
    public interface IRule
    {
        bool IncludeSpec(ISpec spec);
    }
}
