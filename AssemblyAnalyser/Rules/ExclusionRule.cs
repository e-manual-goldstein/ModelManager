using System;
using System.Collections.Generic;
using System.Text;

namespace AssemblyAnalyser
{
    public class ExclusionRule<TSpec> where TSpec : class, ISpec
    {
        Func<TSpec, bool> _excludeFunc;

        public ExclusionRule(Func<TSpec, bool> excludeFunc)
        {
            _excludeFunc = excludeFunc;
        }

        public bool Exclude(TSpec spec)
        {
            return _excludeFunc(spec);
        }
    }

    public class ExclusionRule
    {
        Func<ISpec, bool> _excludeFunc;

        public ExclusionRule(Func<ISpec, bool> excludeFunc)
        {
            _excludeFunc = excludeFunc;
        }

        public bool Exclude(ISpec spec)
        {
            return _excludeFunc(spec);
        }
    }
}
