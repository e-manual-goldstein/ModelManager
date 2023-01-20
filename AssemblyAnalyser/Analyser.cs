using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AssemblyAnalyser
{
    public class Analyser
    {
        #region Type Specs

        ConcurrentDictionary<Type, TypeSpec> _typeSpecs = new ConcurrentDictionary<Type, TypeSpec>();

        public TypeSpec LoadTypeSpec(Type type)
        {
            if (type == null)
            {
                return null;
            }
            lock (_typeSpecs)
            {
                if (!_typeSpecs.TryGetValue(type, out TypeSpec typeSpec))
                {
                    _typeSpecs[type] = typeSpec = new TypeSpec(type);
                }
                return typeSpec;
            }
        }

        public TypeSpec[] LoadTypeSpecs(Type[] types)
        {
            return types.Select(t => LoadTypeSpec(t)).ToArray();
        }

        #endregion

        #region Method Specs

        ConcurrentDictionary<MethodInfo, MethodSpec> _methodSpecs = new ConcurrentDictionary<MethodInfo, MethodSpec>();

        public MethodSpec LoadMethodSpec(MethodInfo method)
        {
            lock (_methodSpecs)
            {
                if (!_methodSpecs.TryGetValue(method, out MethodSpec methodSpec))
                {
                    _methodSpecs[method] = methodSpec = new MethodSpec(method);
                }
                return methodSpec;
            }
        }

        public MethodSpec[] LoadMethodSpecs(MethodInfo[] methodInfos)
        {
            return methodInfos.Select(m => LoadMethodSpec(m)).ToArray();
        }

        #endregion

        #region Property Specs

        ConcurrentDictionary<PropertyInfo, PropertySpec> _propertySpecs = new ConcurrentDictionary<PropertyInfo, PropertySpec>();

        private PropertySpec LoadPropertySpec(PropertyInfo propertyInfo)
        {
            lock (_propertySpecs)
            {
                if (!_propertySpecs.TryGetValue(propertyInfo, out PropertySpec propertySpec))
                {
                    _propertySpecs[propertyInfo] = propertySpec = new PropertySpec(propertyInfo);
                }
                return propertySpec;
            }
        }

        public PropertySpec[] LoadPropertySpecs(PropertyInfo[] propertyInfos)
        {
            return propertyInfos.Select(p => LoadPropertySpec(p)).ToArray();
        }

        public TypeSpec[] Types()
        {
            return _typeSpecs.Values.ToArray();
        }



        #endregion

    }
}
