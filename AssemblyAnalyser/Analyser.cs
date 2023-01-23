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

        #region Parameter Specs

        ConcurrentDictionary<ParameterInfo, ParameterSpec> _parameterSpecs = new ConcurrentDictionary<ParameterInfo, ParameterSpec>();

        private ParameterSpec LoadParameterSpec(ParameterInfo parameterInfo)
        {
            lock (_parameterSpecs)
            {
                if (!_parameterSpecs.TryGetValue(parameterInfo, out ParameterSpec parameterSpec))
                {
                    _parameterSpecs[parameterInfo] = parameterSpec = new ParameterSpec(parameterInfo);
                }
                return parameterSpec;
            }
        }

        public ParameterSpec[] LoadParameterSpecs(ParameterInfo[] parameterInfos)
        {
            return parameterInfos.Select(p => LoadParameterSpec(p)).ToArray();
        }

        #endregion

        #region Field Specs

        ConcurrentDictionary<FieldInfo, FieldSpec> _fieldSpecs = new ConcurrentDictionary<FieldInfo, FieldSpec>();

        private FieldSpec LoadFieldSpec(FieldInfo fieldInfo)
        {
            lock (_fieldSpecs)
            {
                if (!_fieldSpecs.TryGetValue(fieldInfo, out FieldSpec fieldSpec))
                {
                    _fieldSpecs[fieldInfo] = fieldSpec = new FieldSpec(fieldInfo);
                }
                return fieldSpec;
            }
        }

        internal FieldSpec[] LoadFieldSpecs(FieldInfo[] fieldInfos)
        {
            return fieldInfos.Select(f => LoadFieldSpec(f)).ToArray();
        }
        #endregion

    }
}
