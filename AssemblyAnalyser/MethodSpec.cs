using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AssemblyAnalyser
{
    public class MethodSpec
    {
        MethodInfo _methodInfo;

        public MethodSpec(MethodInfo methodInfo)
        {
            _methodInfo = methodInfo;
        }

        public override string ToString()
        {
            return _methodInfo.Name;
        }
    }
}
