using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class Analyser : IDisposable
    {
        readonly string _workingDirectory;
        readonly Dictionary<string, string> _workingFiles;
        readonly ILogger _logger;
        private bool _disposed;
        private readonly MetadataLoadContext _metadataLoadContext;
        public Analyser(string workingDirectory, ILogger logger) 
        {
            _workingDirectory = workingDirectory;
            _workingFiles = Directory.EnumerateFiles(_workingDirectory, "*.dll").ToDictionary(d => Path.GetFileNameWithoutExtension(d), e => e);
            _logger = logger;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            _metadataLoadContext = CreateMetadataContext();
        }

        object _lock = new object();

        public async Task BeginAsync()
        {
            RegisterSpecs();
            await AnalyseAsync();
        }

        public void RegisterSpecs()
        {
            foreach (var (_, filePath) in _workingFiles)
            {
                LoadAssemblyContext(filePath, out Assembly assembly);
                var assemblySpec = LoadAssemblySpec(assembly);
                assemblySpec.Process(this);
            }
        }

        public async Task AnalyseAsync()
        {
            var taskList = new List<Task>();
            foreach (var (_, spec) in _assemblySpecs)
            {
                taskList.Add(spec.AnalyseAsync(this));
            }
            await Task.WhenAll(taskList);
        }

        #region Assembly Specs

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

        private void LoadAssemblyContext(string assemblyName, out Assembly assembly)
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
            var spec = new AssemblySpec(assembly, SpecRules);
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
        
        public bool CanAnalyse(Assembly assembly)
        {
            return _assemblySpecs.TryGetValue(assembly.GetName().Name, out AssemblySpec assemblySpec) && !assemblySpec.Skipped
                && assemblySpec.ReferencedAssemblies.All(s => !s.Skipped);
                //|| assembly.GetReferencedAssemblies().All(r => _workingFiles.Keys.Contains(r.Name));
        }

        #endregion

        #region Type Specs

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

        internal MethodSpec[] TryLoadMethodSpecs(Func<MethodInfo[]> getMethods)
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

        public TypeSpec[] Types()
        {
            return _typeSpecs.Values.ToArray();
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

        #region Rules

        public List<IRule> SpecRules { get; set; } = new List<IRule>();

        #endregion

        public List<string> Report()
        {
            return new List<string>() 
            {  
                $"Assemblies: {_assemblySpecs.Count()}",
                $"Types: {_typeSpecs.Count()}",
                $"Properties {_propertySpecs.Count()}",
                $"Methods {_methodSpecs.Count()}",
                $"Fields {_fieldSpecs.Count()}"
            };
        }

        public List<string> AssemblyReport()
        {
            var groups = _assemblySpecs.Values.Where(spec => spec != AssemblySpec.NullSpec && !spec.Skipped && spec.Analysed)
                .OrderByDescending(c => c.TypeSpecs.Count()).ThenBy(c => c.AssemblyShortName);
            return groups.Select(s => $"{s.AssemblyShortName}: {s.TypeSpecs.Count()}").ToList();
            //    $"Types: {_typeSpecs.Where(key => !key.Value.IsExcluded() && key.Value.IsIncluded()).Count()}\n" +
            //    $"Properties {_propertySpecs.Where(key => !key.Value.IsExcluded() && key.Value.IsIncluded()).Count()}\n" +
            //    $"Methods {_methodSpecs.Where(key => !key.Value.IsExcluded() && key.Value.IsIncluded()).Count()}\n" +
            //    $"Fields {_fieldSpecs.Where(key => !key.Value.IsExcluded() && key.Value.IsIncluded()).Count()}";
        }


        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                _logger.LogError(exception, "Unhandled Exception");
            }
            else
            {
                _logger.LogError("Unhandled Exception");
            }            
        }

        private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is FileNotFoundException fileNotFoundException && _logger != null)
            {
                //HandleMissingFile(fileNotFoundException);
            }
            else
            {
                _logger.LogError(e.Exception, "Unhandled Exception");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                AppDomain.CurrentDomain.FirstChanceException -= CurrentDomain_FirstChanceException;
                _metadataLoadContext.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
