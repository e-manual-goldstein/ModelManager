using System;
using System.Collections.Generic;
using System.Text;

namespace AssemblyAnalyser
{
    public class InclusionRule<TSpec> where TSpec : class, ISpec
    {
        Func<TSpec, bool> _includeFunc;

        public InclusionRule(Func<TSpec, bool> includeFunc)
        {
            _includeFunc = includeFunc;
        }

        public bool Include(TSpec spec)
        {
            return _includeFunc(spec);
        }
    }

    public class InclusionRule : IRule
    {
        Func<ISpec, bool> _includeFunc;

        public InclusionRule(Func<ISpec, bool> includeFunc)
        {
            _includeFunc = includeFunc;
        }

        public bool Include(ISpec spec)
        {
            return _includeFunc(spec);
        }

        public bool IncludeSpec(ISpec spec)
        {
            return Include(spec);
        }
    }
}
