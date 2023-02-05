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
        //private MetadataLoadContext _metadataLoadContext;
        //private PathAssemblyResolver _pathResolver;
        Dictionary<string, string> _workingFiles;
        readonly ILogger _logger;
        readonly IExceptionManager _exceptionManager;
        object _lock = new object();
        private bool _disposed;

        public SpecManager(ILoggerProvider loggerProvider, IExceptionManager exceptionManager)
        {            
            _logger = loggerProvider.CreateLogger("Spec Manager");
            _exceptionManager = exceptionManager;
        }
        
        public List<IRule> SpecRules { get; set; } = new List<IRule>();

        public void SetWorkingDirectory(string workingDirectory)
        {
            _workingFiles = Directory.EnumerateFiles(workingDirectory, "*.dll").ToDictionary(d => Path.GetFileNameWithoutExtension(d), e => e);
            //_pathResolver = CreatePathResolver();
            //_metadataLoadContext = CreateMetadataContext();
        }

        //private MetadataLoadContext CreateMetadataContext()
        //{
        //    return new MetadataLoadContext(_pathResolver);
        //}

        private PathAssemblyResolver CreatePathResolver()
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
            return new PathAssemblyResolver(paths);
        }

        #region Assemblies

        public IReadOnlyDictionary<string, AssemblySpec> Assemblies => _assemblySpecs;

        ConcurrentDictionary<string, AssemblySpec> _assemblySpecs = new ConcurrentDictionary<string, AssemblySpec>();

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

        //public AssemblySpec LoadAssemblySpec(AssemblyName assemblyName)
        //{
        //    AssemblySpec assemblySpec;
        //    if (!_assemblySpecs.TryGetValue(assemblyName.Name, out assemblySpec))
        //    {
        //        lock (_lock)
        //        {
        //            if (!_assemblySpecs.TryGetValue(assemblyName.Name, out assemblySpec))
        //            {
        //                if (TryLoadAssembly(assemblyName, out Assembly assembly))
        //                {
        //                    _assemblySpecs[assemblyName.Name] = assemblySpec = CreateFullAssemblySpec(assembly);
        //                }
        //                else
        //                {
        //                    _assemblySpecs[assemblyName.Name] = assemblySpec = CreatePartialAssemblySpec(assemblyName.Name);
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (assemblyName.ToString() != assemblySpec.AssemblyFullName)
        //        {
        //            assemblySpec.AddRepresentedName(assemblyName.FullName);
        //        }
        //    }
        //    return assemblySpec ?? AssemblySpec.NullSpec;
        //}

        public AssemblySpec[] LoadReferencedAssemblies(string assemblyFullName, string assemblyFilePath)
        {
            var specs = new List<AssemblySpec>();
            //using (var loadContext = CreateMetadataContext())
            //{
            //    var assembly = loadContext.LoadFromAssemblyName(assemblyFullName);
            var loader = AssemblyLoader.GetLoader(null, null);
            var assemblies = loader.LoadReferencedAssembliesByRootPath(assemblyFilePath);
            //var assembly = loader.LoadAssemblyByName(assemblyFullName);

            foreach (var assembly in assemblies)
            {
                try
                {
                    var referencedAssembly = loader.LoadAssemblyByName(assembly.FullName);
                    _assemblySpecs[assembly.GetName().Name] = CreateFullAssemblySpec(referencedAssembly);
                    specs.Add(_assemblySpecs[assembly.GetName().Name]);
                }
                catch (FileNotFoundException ex)
                {
                    _exceptionManager.Handle(ex);
                    _logger.LogWarning($"Unable to load assembly {assembly.GetName().Name}. Required by {assemblyFullName}");
                }
                catch
                {
                    _logger.LogWarning($"Unable to load assembly {assembly.GetName().Name}. Required by {assemblyFullName}");
                }
            }
            //}
            return specs.OrderBy(s => s.FilePath).ToArray();
        }

        //private bool TryLoadAssembly(AssemblyName assemblyName, out Assembly assembly)
        //{
        //    if (_workingFiles.TryGetValue(assemblyName.Name, out string filePath))
        //    {
        //        _logger.Log(LogLevel.Information, $"Loading Working Path Assembly: {assemblyName.Name}");
        //        LoadAssemblyFromPath(filePath, out assembly);
        //        return true;
        //    }
        //    else if (TryLoadSystemAssembly(assemblyName.Name, out assembly))
        //    {
        //        _logger.Log(LogLevel.Information, $"Loading System Assembly: {assemblyName.Name}");
        //        return true;
        //    }
        //    try
        //    {
        //        assembly = _metadataLoadContext.LoadFromAssemblyName(assemblyName);
        //        return true;
        //    }
        //    catch
        //    {
        //        _logger.LogWarning($"Unable to load assembly {assemblyName}");
        //    }
        //    return false;
        //}

        //public void LoadAssemblyFromPath(string assemblyPath, out Assembly assembly)
        //{
        //    assembly = _metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
        //}

        //private bool TryLoadSystemAssembly(string assmemblyName, out Assembly assembly)
        //{
        //    var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
        //    var version = IntPtr.Size == 8 ? "64" : string.Empty;
        //    var dotnetv2Path = Path.Combine(systemFolder, $@"..\Microsoft.NET\Framework{version}\v2.0.50727\{assmemblyName}.dll");
        //    bool exists;
        //    if (exists = File.Exists(dotnetv2Path))
        //    {
        //        assembly = _metadataLoadContext.LoadFromAssemblyPath(dotnetv2Path);
        //    }
        //    else
        //    {
        //        assembly = null;
        //    }
        //    return exists;
        //}

        public AssemblySpec[] LoadAssemblySpecs(Assembly[] types)
        {
            return types.Select(t => LoadAssemblySpec(t)).ToArray();
        }

        public AssemblySpec[] LoadAssemblySpecs(AssemblyName[] assemblyNames)
        {
            throw new NotImplementedException();
            //return assemblyNames.Select(a => LoadAssemblySpec(a)).ToArray();
        }

        private AssemblySpec CreateFullAssemblySpec(Assembly assembly)
        {
            var frameworkVersion = GetTargetFrameworkVersion(assembly);
            var spec = new AssemblySpec(assembly.FullName, assembly.GetName().Name, assembly.Location, this, SpecRules)
            {
                ImageRuntimeVersion = assembly.ImageRuntimeVersion,
                TargetFrameworkVersion = frameworkVersion,
            };
            spec.Logger = _logger;
            return spec;
        }

        private string GetTargetFrameworkVersion(Assembly assembly)
        {
            var attributes = assembly.GetCustomAttributesData();
            foreach (var attributeData in attributes)
            {
                try
                {
                    if (attributeData.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                    {
                        var attributeValue = attributeData.ConstructorArguments[0];
                        return attributeValue.Value.ToString();
                    }
                }
                catch
                {

                }
            }
            return null;
        }

        private AssemblySpec CreatePartialAssemblySpec(string assemblyName)
        {
            var spec = new AssemblySpec(assemblyName, this, SpecRules);
            spec.Exclude("Assembly is only partial spec");
            spec.SkipProcessing("Assembly is only partial spec");
            spec.Logger = _logger;
            return spec;
        }

        #endregion

        #region Types

        public IReadOnlyDictionary<string, TypeSpec> Types => _typeSpecs;

        ConcurrentDictionary<string, TypeSpec> _typeSpecs = new ConcurrentDictionary<string, TypeSpec>();


        public void TryBuildTypeSpecForAssembly(string fullTypeName, AssemblySpec assemblySpec, Action<Type> buildAction)
        {
            Type type = null;
            var loader = AssemblyLoader.GetLoader(assemblySpec.TargetFrameworkVersion, assemblySpec.ImageRuntimeVersion);
            var assembly = loader.LoadAssemblyByName(assemblySpec.AssemblyFullName);
            try
            {
                type = assembly.DefinedTypes.SingleOrDefault(t => t.FullName == fullTypeName);
            }
            catch (ReflectionTypeLoadException ex)
            {
                type = ex.Types.SingleOrDefault(t => t.FullName == fullTypeName);
            }
            if (type != null)
            {
                buildAction(type);
            }

        }

        public TypeSpec[] TryLoadTypesForAssembly(AssemblySpec assemblySpec)
        {
            var specs = new List<TypeSpec>();
            var loader = AssemblyLoader.GetLoader(assemblySpec.TargetFrameworkVersion, assemblySpec.ImageRuntimeVersion);
            var assembly = loader.LoadAssemblyByName(assemblySpec.AssemblyFullName);
            TryLoadTypeSpecs(() => assembly.DefinedTypes.ToArray(), assemblySpec, out TypeSpec[] typeSpecs);
            return typeSpecs;
            
        }

        private TypeSpec LoadTypeSpec(Type type, AssemblySpec assemblySpec)
        {
            if (type == null)
            {
                return TypeSpec.NullSpec;
            }
            return LoadFullTypeSpec(type, assemblySpec);
        }

        private TypeSpec LoadFullTypeSpec(Type type, AssemblySpec assemblySpec)
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
                            _typeSpecs[type.FullName] = typeSpec = CreateFullTypeSpec(type, assemblySpec);
                        }
                    }
                    //Console.WriteLine($"Unlocking for {type.FullName}");
                }
            }
            return typeSpec;
        }

        private TypeSpec CreateFullTypeSpec(Type type, AssemblySpec assemblySpec)
        {
            var spec = new TypeSpec(type.FullName, type.IsInterface, this, SpecRules);
            spec.Logger = _logger;
            spec.Assembly = assemblySpec;
            return spec;
        }

        private TypeSpec LoadPartialTypeSpec(string typeName, AssemblySpec assemblySpec)
        {
            TypeSpec typeSpec = TypeSpec.NullSpec;
            if (!_typeSpecs.TryGetValue(typeName, out typeSpec))
            {
                //Console.WriteLine($"Locking for {typeName}");
                lock (_lock)
                {
                    if (!_typeSpecs.TryGetValue(typeName, out typeSpec))
                    {
                        _typeSpecs[typeName] = typeSpec = CreatePartialTypeSpec(typeName, assemblySpec);
                    }
                }
                //Console.WriteLine($"Unlocking for {typeName}");
            }
            return typeSpec;
        }

        private TypeSpec CreatePartialTypeSpec(string typeName, AssemblySpec assemblySpec)
        {
            var spec = new TypeSpec(typeName, this, SpecRules);
            spec.Exclude("Type is only partial spec");
            spec.SkipProcessing("Type is only partial spec");
            spec.Logger = _logger;
            spec.Assembly = assemblySpec;
            return spec;
        }

        public bool TryLoadTypeSpec(Func<Type> getType, AssemblySpec assemblySpec, out TypeSpec typeSpec)
        {
            bool success = false;
            typeSpec = TypeSpec.NullSpec;
            try
            {
                typeSpec = LoadTypeSpec(getType(), assemblySpec);
                success = true;
            }
            catch (TypeLoadException ex)
            {
                if (!string.IsNullOrEmpty(ex.TypeName))
                {
                    typeSpec = LoadPartialTypeSpec(ex.TypeName, assemblySpec);
                    success = true;
                }                
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");            
            }
            catch (Exception ex)
            {
                
            }
            return success;
        }

        public bool TryLoadTypeSpecs(Func<Type[]> getTypes, AssemblySpec assemblySpec, out TypeSpec[] typeSpecs)
        {
            typeSpecs = Array.Empty<TypeSpec>();
            bool success = false;
            try
            { 
                typeSpecs = LoadTypeSpecs(getTypes(), assemblySpec);
                success = true;
            }
            catch (TypeLoadException ex)
            {
                _logger.LogError(ex.Message);            
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex.Message);
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
                if (ex.Types.Any())
                {
                    success = true;
                    typeSpecs = LoadTypeSpecs(ex.Types, assemblySpec);
                }
            }
            return success;
        }

        public TypeSpec[] LoadTypeSpecs(Type[] types, AssemblySpec assemblySpec)
        {
            return types.Select(t => LoadTypeSpec(t, assemblySpec)).ToArray();
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
                        _methodSpecs[method] = methodSpec = new MethodSpec(method, this, SpecRules);
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
                _exceptionManager.Handle(ex);
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
                        _propertySpecs[propertyInfo] = propertySpec = new PropertySpec(propertyInfo, this, SpecRules);
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
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");
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
                        _parameterSpecs[parameterInfo] = parameterSpec = new ParameterSpec(parameterInfo, this, SpecRules);
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
            catch (TypeLoadException typeLoadException)
            {

            }
            catch (Exception)
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
                        _fieldSpecs[fieldInfo] = fieldSpec = new FieldSpec(fieldInfo, this, SpecRules);
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
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");
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
