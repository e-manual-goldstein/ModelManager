using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AssemblyAnalyser
{
    public class PropertySpec
    {
        private PropertyInfo _propertyInfo;
        private MethodInfo _getter;
        private MethodInfo _setter;

        public PropertySpec(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
            _getter = propertyInfo.GetGetMethod();
            _setter = propertyInfo.GetSetMethod();
        }

        public IEnumerable<MethodInfo> InnerMethods()
        {
            return new[] { _getter, _setter };
        }
    }
}
