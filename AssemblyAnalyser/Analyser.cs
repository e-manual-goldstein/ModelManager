using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        object _lock = new object();

        public async Task BeginAsync()
        {
            var taskList = new List<Task>();
            foreach (var (_, filePath) in _workingFiles)
            {
                var assemblySpec = LoadAssemblySpec(Assembly.LoadFrom(filePath));
                taskList.Add(assemblySpec.AnalyseAsync(this));
            }
            await Task.WhenAll(taskList);
        }

        #region Assembly Specs

        List<ExclusionRule<AssemblySpec>> _assemblyExclusions = new List<ExclusionRule<AssemblySpec>>();
        List<InclusionRule<AssemblySpec>> _assemblyInclusions = new List<InclusionRule<AssemblySpec>>();

        ConcurrentDictionary<string, AssemblySpec> _assemblySpecs = new ConcurrentDictionary<string, AssemblySpec>();

        public AssemblySpec LoadAssemblySpec(Assembly assembly)
        {
            AssemblySpec assemblySpec = null;
            if (assembly == null)
            {
                return null;
            }
            if (!_assemblySpecs.TryGetValue(assembly.FullName, out assemblySpec))
            {
                Console.WriteLine($"Locking for {assembly.FullName}");
                lock (_lock)
                {
                    if (!_assemblySpecs.TryGetValue(assembly.FullName, out assemblySpec))
                    {
                        _assemblySpecs[assembly.FullName] = assemblySpec = CreateFullAssemblySpec(assembly);
                    }
                }
                Console.WriteLine($"Unlocking for {assembly.FullName}");
            }
            return assemblySpec;
        }

        public AssemblySpec LoadAssemblySpec(AssemblyName assemblyName)
        {
            AssemblySpec assemblySpec = null;
            if (!_assemblySpecs.TryGetValue(assemblyName.FullName, out assemblySpec))
            {
                Console.WriteLine($"Locking for {assemblyName}");
                lock (_lock)
                {
                    if (!_assemblySpecs.TryGetValue(assemblyName.FullName, out assemblySpec))
                    {
                        if (TryLoadAssembly(assemblyName, out Assembly assembly))
                        {
                            _assemblySpecs[assemblyName.FullName] = assemblySpec = CreateFullAssemblySpec(assembly);
                        }
                        else
                        {
                            _assemblySpecs[assemblyName.FullName] = assemblySpec = CreatePartialAssemblySpec(assemblyName.FullName);
                        }
                    }
                }
                Console.WriteLine($"Unlocking for {assemblyName}");
            }
            return assemblySpec;
        }

        private bool TryLoadAssembly(AssemblyName assemblyName, out Assembly assembly)
        {
            assembly = null;
            try
            {
                if (_workingFiles.TryGetValue(assemblyName.Name, out string filePath))
                {
                    var candidateAssembly = Assembly.LoadFrom(filePath);
                    if (candidateAssembly.FullName == assemblyName.FullName)
                    {
                        assembly = candidateAssembly;
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Incorrect Assembly Version");
                    }
                }                
            }
            catch (FileNotFoundException)
            {
                
            }
            return false;
        }

        private bool TryLoadAssembly(string assemblyName, out Assembly assembly)
        {
            assembly = null;
            try
            {
                if (_workingFiles.TryGetValue(assemblyName, out string filePath))
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

        private AssemblySpec CreateFullAssemblySpec(Assembly assembly)
        {
            var spec = new AssemblySpec(assembly);
            spec.ExclusionRules.AddRange(_assemblyExclusions);
            spec.InclusionRules.AddRange(_assemblyInclusions);
            return spec;
        }

        private AssemblySpec CreatePartialAssemblySpec(string assemblyName)
        {
            return new AssemblySpec(assemblyName);
        }

        #endregion

        #region Type Specs

        ConcurrentDictionary<string, TypeSpec> _typeSpecs = new ConcurrentDictionary<string, TypeSpec>();

        private TypeSpec LoadTypeSpec(Type type)
        {
            TypeSpec typeSpec = null;
            if (type == null)
            {
                return typeSpec;
            }
            return LoadFullTypeSpec(type);
        }

        private TypeSpec LoadFullTypeSpec(Type type)
        {
            TypeSpec typeSpec = null;
            if (!string.IsNullOrEmpty(type.FullName))
            {
                if (!_typeSpecs.TryGetValue(type.FullName, out typeSpec))
                {
                    Console.WriteLine($"Locking for {type.FullName}");
                    lock (_lock)
                    {
                        if (!_typeSpecs.TryGetValue(type.FullName, out typeSpec))
                        {
                            _typeSpecs[type.FullName] = typeSpec = new TypeSpec(type);
                        }
                    }
                    Console.WriteLine($"Unlocking for {type.FullName}");
                }
            }
            return typeSpec;
        }

        private TypeSpec LoadPartialTypeSpec(string typeName)
        {
            TypeSpec typeSpec = null;
            if (!_typeSpecs.TryGetValue(typeName, out typeSpec))
            {
                Console.WriteLine($"Locking for {typeName}");
                lock (_lock)
                {
                    if (!_typeSpecs.TryGetValue(typeName, out typeSpec))
                    {
                        _typeSpecs[typeName] = typeSpec = new TypeSpec(typeName);
                    }
                }
                Console.WriteLine($"Unlocking for {typeName}");
            }
            return typeSpec;
        }

        public TypeSpec TryLoadTypeSpec(Func<Type> propertyTypeFunc)
        {
            Type type = null;
            try
            {
                type = propertyTypeFunc();
            }
            catch (TypeLoadException ex)
            {
                return LoadPartialTypeSpec(ex.TypeName);
            }
            return LoadTypeSpec(type);
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
            MethodSpec methodSpec = null;
            if (method == null)
            {
                return null;
            }
            if (!_methodSpecs.TryGetValue(method, out methodSpec))
            {
                Console.WriteLine($"Locking for {method.Name}");
                lock (_lock)
                {
                    if (!_methodSpecs.TryGetValue(method, out methodSpec))
                    {
                        _methodSpecs[method] = methodSpec = new MethodSpec(method);
                    }
                }
                Console.WriteLine($"Unlocking for {method.Name}");
            }
            return methodSpec;
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
            PropertySpec propertySpec = null;
            if (!_propertySpecs.TryGetValue(propertyInfo, out propertySpec))
            {
                Console.WriteLine($"Locking for {propertyInfo.Name}");
                lock (_lock)
                {
                    if (!_propertySpecs.TryGetValue(propertyInfo, out propertySpec))
                    {
                        _propertySpecs[propertyInfo] = propertySpec = new PropertySpec(propertyInfo);
                    }
                }
            }
            Console.WriteLine($"Unlocking for {propertyInfo.Name}");
            return propertySpec;
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
            ParameterSpec parameterSpec = null;
            if (!_parameterSpecs.TryGetValue(parameterInfo, out parameterSpec))
            {
                Console.WriteLine($"Locking for {parameterInfo.Name}");
                lock (_lock)
                {
                    if (!_parameterSpecs.TryGetValue(parameterInfo, out parameterSpec))
                    {
                        _parameterSpecs[parameterInfo] = parameterSpec = new ParameterSpec(parameterInfo);
                    }
                }
                Console.WriteLine($"Unlocking for {parameterInfo.Name}");
            }
            return parameterSpec;
        }

        public ParameterSpec[] LoadParameterSpecs(ParameterInfo[] parameterInfos)
        {
            return parameterInfos?.Select(p => LoadParameterSpec(p)).ToArray();
        }

        public ParameterSpec[] TryLoadParameterSpecs(Func<ParameterInfo[]> parameterInfosFunc)
        {
            ParameterInfo[] parameterInfos = null;
            try
            {
                parameterInfos = parameterInfosFunc();
            }
            catch (TypeLoadException)
            {

            }
            return LoadParameterSpecs(parameterInfos);
        }

        #endregion

        #region Field Specs

        ConcurrentDictionary<FieldInfo, FieldSpec> _fieldSpecs = new ConcurrentDictionary<FieldInfo, FieldSpec>();


        private FieldSpec LoadFieldSpec(FieldInfo fieldInfo)
        {
            FieldSpec fieldSpec = null;
            if (!_fieldSpecs.TryGetValue(fieldInfo, out fieldSpec))
            {
                Console.WriteLine($"Locking for {fieldInfo.Name}");
                lock (_lock)
                {
                    if (!_fieldSpecs.TryGetValue(fieldInfo, out fieldSpec))
                    {
                        _fieldSpecs[fieldInfo] = fieldSpec = new FieldSpec(fieldInfo);
                    }
                }
                Console.WriteLine($"Unlocking for {fieldInfo.Name}");
            }            
            return fieldSpec;
        }

        internal FieldSpec[] LoadFieldSpecs(FieldInfo[] fieldInfos)
        {
            return fieldInfos.Select(f => LoadFieldSpec(f)).ToArray();
        }

        #endregion

        public void ExcludeAssembly(string assemblyName)
        {
            _assemblyExclusions.Add(new ExclusionRule<AssemblySpec>(spec =>
            {
                return spec.AssemblyName == assemblyName;
            }));
        }

        public void IncludeAssembly(string assemblyName)
        {
            _assemblyInclusions.Add(new InclusionRule<AssemblySpec>(spec =>
            {
                return spec.AssemblyName == assemblyName;
            }));
        }

        public string Report()
        {
            return $"Assemblies: {_assemblySpecs.Count}\n" +
                $"Types: {_typeSpecs.Count}\n" +
                $"Properties {_propertySpecs.Count}\n" +
                $"Methods {_methodSpecs.Count}\n" +
                $"Fields {_fieldSpecs.Count}";
        }

    }
}
