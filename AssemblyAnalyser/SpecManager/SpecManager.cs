using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class SpecManager : ISpecManager
    {
        private MetadataLoadContext _metadataLoadContext;
        Dictionary<string, string> _workingFiles;
        readonly ILogger _logger;
        object _lock = new object();
        private bool _disposed;

        public SpecManager(ILoggerProvider loggerProvider)
        {            
            _logger = loggerProvider.CreateLogger("Spec Manager");
        }
        
        public List<IRule> SpecRules { get; set; } = new List<IRule>();

        public void SetWorkingDirectory(string workingDirectory)
        {
            _workingFiles = Directory.EnumerateFiles(workingDirectory, "*.dll").ToDictionary(d => Path.GetFileNameWithoutExtension(d), e => e);
            _metadataLoadContext = CreateMetadataContext();
        }

        
        #region Assemblies

        public IReadOnlyDictionary<string, AssemblySpec> Assemblies => _assemblySpecs;

        ConcurrentDictionary<string, AssemblySpec> _assemblySpecs = new ConcurrentDictionary<string, AssemblySpec>();

        public List<string> ListAssemblySpecs => _assemblySpecs.Values.Select(s => s.FilePath).ToList();

        public AssemblySpec LoadAssemblySpec(Assembly assembly)
        {
            AssemblySpec assemblySpec;
            if (assembly == null)
            {
                return AssemblySpec.NullSpec;
            }
            if (!_assemblySpecs.TryGetValue(assembly.GetName().Name, out assemblySpec))
            {
                //Console.WriteLine($"Locking for {assembly.FullName}");
                lock (_lock)
                {
                    if (!_assemblySpecs.TryGetValue(assembly.GetName().Name, out assemblySpec))
                    {
                        _assemblySpecs[assembly.GetName().Name] = assemblySpec = CreateFullAssemblySpec(assembly);
                    }
                }
                //Console.WriteLine($"Unlocking for {assembly.FullName}");
            }
            assemblySpec ??= AssemblySpec.NullSpec;
            return assemblySpec;
        }

        public AssemblySpec LoadAssemblySpec(AssemblyName assemblyName)
        {
            AssemblySpec assemblySpec;
            if (!_assemblySpecs.TryGetValue(assemblyName.Name, out assemblySpec))
            {
                lock (_lock)
                {
                    if (!_assemblySpecs.TryGetValue(assemblyName.Name, out assemblySpec))
                    {
                        if (TryLoadAssembly(assemblyName, out Assembly assembly))
                        {
                            _assemblySpecs[assemblyName.Name] = assemblySpec = CreateFullAssemblySpec(assembly);
                        }
                        else
                        {
                            _assemblySpecs[assemblyName.Name] = assemblySpec = CreatePartialAssemblySpec(assemblyName.Name);
                        }
                    }
                }
            }
            else
            {
                if (assemblyName.ToString() != assemblySpec.AssemblyFullName)
                {
                    assemblySpec.AddRepresentedName(assemblyName);
                }
            }
            return assemblySpec ?? AssemblySpec.NullSpec;
        }

        private bool TryLoadAssembly(AssemblyName assemblyName, out Assembly assembly)
        {
            if (_workingFiles.TryGetValue(assemblyName.Name, out string filePath))
            {
                _logger.Log(LogLevel.Information, $"Loading Working Path Assembly: {assemblyName.Name}");
                LoadAssemblyContext(filePath, out assembly);
                return true;
            }
            else if (TryLoadSystemAssembly(assemblyName.Name, out assembly))
            {
                _logger.Log(LogLevel.Information, $"Loading System Assembly: {assemblyName.Name}");
                return true;
            }
            try
            {
                assembly = _metadataLoadContext.LoadFromAssemblyName(assemblyName);
                return true;
            }
            catch
            {
                _logger.LogWarning($"Unable to load assembly {assemblyName}");
            }
            return false;
        }

        private MetadataLoadContext CreateMetadataContext()
        {
            // Get the array of runtime assemblies.
            string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");

            // Create the list of assembly paths consisting of runtime assemblies and the inspected assembly.
            var paths = new List<string>(runtimeAssemblies);
            paths.AddRange(_workingFiles.Values);
            paths.AddRange(Directory.GetFiles("C:\\WINDOWS\\assembly", "*.dll"));
            var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var version = IntPtr.Size == 8 ? "64" : string.Empty;
            var frameworkPaths = Directory.GetFiles(Path.Combine(systemFolder, $@"..\Microsoft.NET\Framework{version}\v2.0.50727\"), "*.dll");
            paths.AddRange(frameworkPaths);
            // Create PathAssemblyResolver that can resolve assemblies using the created list.
            var resolver = new PathAssemblyResolver(paths);
            return new MetadataLoadContext(resolver);
        }

        public void LoadAssemblyContext(string assemblyName, out Assembly assembly)
        {
            assembly = _metadataLoadContext.LoadFromAssemblyPath(assemblyName);
        }

        private bool TryLoadSystemAssembly(string assmemblyName, out Assembly assembly)
        {
            var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var version = IntPtr.Size == 8 ? "64" : string.Empty;
            var dotnetv2Path = Path.Combine(systemFolder, $@"..\Microsoft.NET\Framework{version}\v2.0.50727\{assmemblyName}.dll");
            bool exists;
            if (exists = File.Exists(dotnetv2Path))
            {
                assembly = _metadataLoadContext.LoadFromAssemblyPath(dotnetv2Path);
            }
            else
            {
                assembly = null;
            }
            return exists;
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
            var spec = new AssemblySpec(assembly, this, SpecRules);
            spec.Logger = _logger;
            return spec;
        }

        private AssemblySpec CreatePartialAssemblySpec(string assemblyName)
        {
            var spec = new AssemblySpec(assemblyName, SpecRules);
            spec.Exclude("Assembly is only partial spec");
            spec.SkipProcessing("Assembly is only partial spec");
            spec.Logger = _logger;
            return spec;
        }

        #endregion

        #region Types

        public IReadOnlyDictionary<string, TypeSpec> Types => _typeSpecs;

        ConcurrentDictionary<string, TypeSpec> _typeSpecs = new ConcurrentDictionary<string, TypeSpec>();

        private TypeSpec LoadTypeSpec(Type type)
        {
            if (type == null)
            {
                return TypeSpec.NullSpec;
            }
            return LoadFullTypeSpec(type);
        }

        private TypeSpec LoadFullTypeSpec(Type type)
        {
            TypeSpec typeSpec = TypeSpec.NullSpec;
            if (!string.IsNullOrEmpty(type.FullName))
            {
                if (!_typeSpecs.TryGetValue(type.FullName, out typeSpec))
                {
                    //Console.WriteLine($"Locking for {type.FullName}");
                    lock (_lock)
                    {
                        if (!_typeSpecs.TryGetValue(type.FullName, out typeSpec))
                        {
                            _typeSpecs[type.FullName] = typeSpec = CreateFullTypeSpec(type);
                        }
                    }
                    //Console.WriteLine($"Unlocking for {type.FullName}");
                }
            }
            return typeSpec;
        }

        private TypeSpec CreateFullTypeSpec(Type type)
        {
            var spec = new TypeSpec(type, SpecRules);
            spec.Logger = _logger;
            return spec;
        }

        private TypeSpec LoadPartialTypeSpec(string typeName)
        {
            TypeSpec typeSpec = TypeSpec.NullSpec;
            if (!_typeSpecs.TryGetValue(typeName, out typeSpec))
            {
                //Console.WriteLine($"Locking for {typeName}");
                lock (_lock)
                {
                    if (!_typeSpecs.TryGetValue(typeName, out typeSpec))
                    {
                        _typeSpecs[typeName] = typeSpec = CreatePartialTypeSpec(typeName);
                    }
                }
                //Console.WriteLine($"Unlocking for {typeName}");
            }
            return typeSpec;
        }

        private TypeSpec CreatePartialTypeSpec(string typeName)
        {
            var spec = new TypeSpec(typeName, SpecRules);
            spec.Exclude("Type is only partial spec");
            spec.SkipProcessing("Type is only partial spec");
            spec.Logger = _logger;
            return spec;
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

        public TypeSpec[] TryLoadTypeSpecs(Func<Type[]> getTypes)
        {
            Type[] types;
            try
            {
                types = getTypes();
            }
            catch (TypeLoadException ex)
            {
                _logger.LogError(ex.Message);
                types = Array.Empty<Type>();
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex.Message);
                types = Array.Empty<Type>();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
                types = ex.Types.ToArray();
            }
            return LoadTypeSpecs(types);
        }

        public TypeSpec[] LoadTypeSpecs(Type[] types)
        {
            return types.Select(t => LoadTypeSpec(t)).ToArray();
        }

        #endregion

        #region Method Specs
        
        public IReadOnlyDictionary<MethodInfo, MethodSpec> Methods => _methodSpecs;

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
                //Console.WriteLine($"Locking for {method.Name}");
                lock (_lock)
                {
                    if (!_methodSpecs.TryGetValue(method, out methodSpec))
                    {
                        _methodSpecs[method] = methodSpec = new MethodSpec(method, SpecRules);
                    }
                }
                //Console.WriteLine($"Unlocking for {method.Name}");
            }
            return methodSpec;
        }

        public MethodSpec[] LoadMethodSpecs(MethodInfo[] methodInfos)
        {
            return methodInfos.Select(m => LoadMethodSpec(m)).ToArray();
        }

        public MethodSpec[] TryLoadMethodSpecs(Func<MethodInfo[]> getMethods)
        {
            MethodInfo[] methods = null;
            try
            {
                methods = getMethods();
            }
            catch (TypeLoadException ex)
            {
                _logger.LogError(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex.Message);
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
            }
            finally
            {
                methods ??= Array.Empty<MethodInfo>();
            }
            return LoadMethodSpecs(methods);
        }

        #endregion

        #region Property Specs
        
        public IReadOnlyDictionary<PropertyInfo, PropertySpec> Properties => _propertySpecs;

        ConcurrentDictionary<PropertyInfo, PropertySpec> _propertySpecs = new ConcurrentDictionary<PropertyInfo, PropertySpec>();

        private PropertySpec LoadPropertySpec(PropertyInfo propertyInfo)
        {
            PropertySpec propertySpec;
            if (!_propertySpecs.TryGetValue(propertyInfo, out propertySpec))
            {
                //Console.WriteLine($"Locking for {propertyInfo.Name}");
                lock (_lock)
                {
                    if (!_propertySpecs.TryGetValue(propertyInfo, out propertySpec))
                    {
                        _propertySpecs[propertyInfo] = propertySpec = new PropertySpec(propertyInfo, SpecRules);
                    }
                }
                //Console.WriteLine($"Unlocking for {propertyInfo.Name}");
            }
            return propertySpec;
        }

        public PropertySpec[] LoadPropertySpecs(PropertyInfo[] propertyInfos)
        {
            return propertyInfos.Select(p => LoadPropertySpec(p)).ToArray();
        }

        public PropertySpec[] TryLoadPropertySpecs(Func<PropertyInfo[]> getProperties)
        {
            PropertyInfo[] properties = null;
            try
            {
                properties = getProperties();
            }
            catch (TypeLoadException ex)
            {
                _logger.LogError(ex.Message);
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
            }
            finally
            {
                properties ??= Array.Empty<PropertyInfo>();
            }
            return LoadPropertySpecs(properties);
        }


        #endregion

        #region Parameter Specs

        public IReadOnlyDictionary<ParameterInfo, ParameterSpec> Parameters => _parameterSpecs;

        ConcurrentDictionary<ParameterInfo, ParameterSpec> _parameterSpecs = new ConcurrentDictionary<ParameterInfo, ParameterSpec>();

        private ParameterSpec LoadParameterSpec(ParameterInfo parameterInfo)
        {
            ParameterSpec parameterSpec = null;
            if (!_parameterSpecs.TryGetValue(parameterInfo, out parameterSpec))
            {
                //Console.WriteLine($"Locking for {parameterInfo.Name}");
                lock (_lock)
                {
                    if (!_parameterSpecs.TryGetValue(parameterInfo, out parameterSpec))
                    {
                        _parameterSpecs[parameterInfo] = parameterSpec = new ParameterSpec(parameterInfo, SpecRules);
                    }
                }
                //Console.WriteLine($"Unlocking for {parameterInfo.Name}");
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

        public IReadOnlyDictionary<FieldInfo, FieldSpec> Fields => _fieldSpecs;

        ConcurrentDictionary<FieldInfo, FieldSpec> _fieldSpecs = new ConcurrentDictionary<FieldInfo, FieldSpec>();

        private FieldSpec LoadFieldSpec(FieldInfo fieldInfo)
        {
            FieldSpec fieldSpec = null;
            if (!_fieldSpecs.TryGetValue(fieldInfo, out fieldSpec))
            {
                //Console.WriteLine($"Locking for {fieldInfo.Name}");
                lock (_lock)
                {
                    if (!_fieldSpecs.TryGetValue(fieldInfo, out fieldSpec))
                    {
                        _fieldSpecs[fieldInfo] = fieldSpec = new FieldSpec(fieldInfo, SpecRules);
                    }
                }
                //Console.WriteLine($"Unlocking for {fieldInfo.Name}");
            }
            return fieldSpec;
        }

        public FieldSpec[] LoadFieldSpecs(FieldInfo[] fieldInfos)
        {
            return fieldInfos.Select(f => LoadFieldSpec(f)).ToArray();
        }

        public FieldSpec[] TryLoadFieldSpecs(Func<FieldInfo[]> getFields)
        {
            FieldInfo[] fields = null;
            try
            {
                fields = getFields();
            }
            catch (TypeLoadException ex)
            {
                _logger.LogError(ex.Message);
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
            }
            finally
            {
                fields ??= Array.Empty<FieldInfo>();
            }
            return LoadFieldSpecs(fields);
        }

        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _metadataLoadContext.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        } 
        #endregion

    }
}
