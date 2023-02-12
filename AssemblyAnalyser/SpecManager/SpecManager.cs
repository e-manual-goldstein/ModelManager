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
            assemblySpec = _assemblySpecs.GetOrAdd(assembly.FullName, (key) => CreateFullAssemblySpec(assembly));            
            return assemblySpec;
        }

        public AssemblySpec[] LoadReferencedAssemblies(string assemblyFullName, string assemblyFilePath, string targetFrameworkVersion = null, string imageRuntimeVersion = null)
        {
            var specs = new List<AssemblySpec>();
            var loader = AssemblyLoader.GetLoader(targetFrameworkVersion, imageRuntimeVersion);
            var assemblyNames = loader.PreLoadReferencedAssembliesByRootPath(assemblyFilePath);
            foreach (var assemblyName in assemblyNames)
            {
                try
                {
                    var assemblySpec = _assemblySpecs.GetOrAdd(assemblyName, (name) =>
                    {
                        var assembly = loader.LoadAssemblyByName(assemblyName);
                        return CreateFullAssemblySpec(assembly);
                    });
                    specs.Add(assemblySpec);
                }
                catch (FileNotFoundException ex)
                {
                    _exceptionManager.Handle(ex);
                    _logger.LogWarning($"Unable to load assembly {assemblyName}. Required by {assemblyFullName}");
                }
                catch
                {
                    _logger.LogWarning($"Unable to load assembly {assemblyName}. Required by {assemblyFullName}");
                }
            }
            return specs.OrderBy(s => s.FilePath).ToArray();
        }

        public AssemblySpec[] LoadAssemblySpecs(Assembly[] types)
        {
            return types.Select(t => LoadAssemblySpec(t)).ToArray();
        }

        private AssemblySpec CreateFullAssemblySpec(Assembly assembly)
        {
            assembly.TryGetTargetFrameworkVersion(out string frameworkVersion);
            var spec = new AssemblySpec(assembly.FullName, assembly.GetName().Name, assembly.Location, this, SpecRules)
            {
                ImageRuntimeVersion = assembly.ImageRuntimeVersion,
                TargetFrameworkVersion = frameworkVersion,
            };
            
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
            TryLoadTypeSpecs(() => assembly.DefinedTypes.ToArray(), out TypeSpec[] typeSpecs);
            return typeSpecs;
        }

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
                typeSpec = _typeSpecs.GetOrAdd(type.FullName, (key) => CreateFullTypeSpec(type));                
            }
            return typeSpec;
        }

        private TypeSpec CreateFullTypeSpec(Type type)
        {
            var spec = new TypeSpec(type.FullName, type.IsInterface, this, SpecRules);
            spec.Logger = _logger;
            spec.Assembly = _assemblySpecs[type.Assembly.FullName];
            return spec;
        }

        private TypeSpec LoadPartialTypeSpec(string typeName)
        {
            return _typeSpecs.GetOrAdd(typeName, (key) => CreatePartialTypeSpec(typeName));            
        }

        private TypeSpec CreatePartialTypeSpec(string typeName)
        {
            var spec = new TypeSpec(typeName, this, SpecRules);
            spec.Exclude("Type is only partial spec");
            spec.SkipProcessing("Type is only partial spec");
            spec.Logger = _logger;            
            return spec;
        }

        public bool TryLoadTypeSpec(Func<Type> getType, out TypeSpec typeSpec)
        {
            bool success = false;
            typeSpec = TypeSpec.NullSpec;
            try
            {
                typeSpec = LoadTypeSpec(getType());
                success = true;
            }
            catch (TypeLoadException ex)
            {
                if (!string.IsNullOrEmpty(ex.TypeName))
                {
                    typeSpec = LoadPartialTypeSpec(ex.TypeName);
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

        public bool TryLoadTypeSpecs(Func<Type[]> getTypes, out TypeSpec[] typeSpecs)
        {
            typeSpecs = Array.Empty<TypeSpec>();
            bool success = false;
            try
            { 
                typeSpecs = LoadTypeSpecs(getTypes());
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
                    typeSpecs = LoadTypeSpecs(ex.Types);
                }
            }
            return success;
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
            if (method == null)
            {
                return null;
            }
            return _methodSpecs.GetOrAdd(method, (key) => CreateMethodSpec(method));            
        }

        private MethodSpec CreateMethodSpec(MethodInfo method)
        {
            var spec = new MethodSpec(method, this, SpecRules);
            spec.DeclaringType = LoadFullTypeSpec(method.DeclaringType);
            
            return spec;
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
                _propertySpecs[propertyInfo] = propertySpec = new PropertySpec(propertyInfo, this, SpecRules);
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
            return _parameterSpecs.GetOrAdd(parameterInfo, new ParameterSpec(parameterInfo, this, SpecRules));                        
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
