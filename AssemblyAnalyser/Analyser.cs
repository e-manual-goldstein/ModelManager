using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AssemblyAnalyser
{
    public class Analyser
    {
        readonly string _workingDirectory;
        readonly Dictionary<string, string> _workingFiles;

        public Analyser(string workingDirectory) 
        {
            _workingDirectory = workingDirectory;
            _workingFiles = Directory.EnumerateFiles(_workingDirectory, "*.dll").ToDictionary(d => Path.GetFileNameWithoutExtension(d), e => e);
        }

        #region Assembly Specs
        object _lock = new object();
        ConcurrentDictionary<string, AssemblySpec> _assemblySpecs = new ConcurrentDictionary<string, AssemblySpec>();

        public AssemblySpec LoadAssemblySpec(Assembly assembly)
        {
            if (assembly == null)
            {
                return null;
            }
            lock (_lock)
            {
                if (!_assemblySpecs.TryGetValue(assembly.FullName, out AssemblySpec assemblySpec))
                {
                    _assemblySpecs[assembly.FullName] = assemblySpec = new AssemblySpec(assembly);
                }
                return assemblySpec;
            }
        }

        public AssemblySpec LoadAssemblySpec(AssemblyName assemblyName)
        {
            lock (_lock)
            {
                if (!_assemblySpecs.TryGetValue(assemblyName.FullName, out AssemblySpec assemblySpec))
                {
                    if (TryLoadAssembly(assemblyName, out Assembly assembly))
                    {
                        _assemblySpecs[assemblyName.FullName] = assemblySpec = new AssemblySpec(assembly);
                    }
                    else
                    {
                        _assemblySpecs[assemblyName.FullName] = assemblySpec = new AssemblySpec(assemblyName.FullName);
                    }
                }
                return assemblySpec;
            }
        }

        private bool TryLoadAssembly(AssemblyName assemblyName, out Assembly assembly)
        {
            assembly = null;
            try
            {
                if (_workingFiles.TryGetValue(assemblyName.Name, out string filePath))
                {
                    assembly = Assembly.LoadFrom(filePath);
                    return true;
                }                
            }
            catch (FileNotFoundException)
            {
                
            }
            return false;
        }

        public AssemblySpec[] LoadAssemblySpecs(Assembly[] types)
        {
            return types.Select(t => LoadAssemblySpec(t)).ToArray();
        }

        public AssemblySpec[] LoadAssemblySpecs(AssemblyName[] assemblyNames)
        {
            return assemblyNames.Select(a => LoadAssemblySpec(a)).ToArray();
        }

        #endregion

        #region Type Specs

        ConcurrentDictionary<Type, TypeSpec> _typeSpecs = new ConcurrentDictionary<Type, TypeSpec>();

        public TypeSpec LoadTypeSpec(Type type)
        {
            if (type == null)
            {
                return null;
            }
            lock (_lock)
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
            lock (_lock)
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
            lock (_lock)
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
