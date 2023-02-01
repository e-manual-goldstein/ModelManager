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
        readonly  ISpecManager _specManager;
        private bool _disposed;
        
        public Analyser(string workingDirectory, ILogger logger, ISpecManager specManager) 
        {
            _specManager = specManager;
            specManager.SetWorkingDirectory(workingDirectory);
            _workingDirectory = workingDirectory;
            _workingFiles = Directory.EnumerateFiles(_workingDirectory, "*.dll").ToDictionary(d => Path.GetFileNameWithoutExtension(d), e => e);
            _logger = logger;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
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
                _specManager.LoadAssemblyContext(filePath, out Assembly assembly);
                var assemblySpec = _specManager.LoadAssemblySpec(assembly);
                assemblySpec.Process(this, _specManager);
            }
        }

        public async Task AnalyseAsync()
        {
            var taskList = new List<Task>();
            foreach (var (_, spec) in _specManager.Assemblies)
            {
                taskList.Add(spec.AnalyseAsync(this));
            }
            await Task.WhenAll(taskList);
        }

        #region Assembly Specs

        public AssemblySpec Process(Assembly assembly)
        {
            return _specManager.LoadAssemblySpec(assembly);            
        }

        public bool CanAnalyse(Assembly assembly)
        {
            return _specManager.Assemblies.TryGetValue(assembly.GetName().Name, out AssemblySpec assemblySpec) && !assemblySpec.Skipped
                && assemblySpec.ReferencedAssemblies.All(s => !s.Skipped);
                //|| assembly.GetReferencedAssemblies().All(r => _workingFiles.Keys.Contains(r.Name));
        }

        #endregion

        #region Type Specs



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
            return _specManager.Types.Values.ToArray();
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
                $"Assemblies: {_specManager.Assemblies.Count()}",
                $"Types: {_specManager.Types.Count()}",
                $"Properties {_propertySpecs.Count()}",
                $"Methods {_methodSpecs.Count()}",
                $"Fields {_fieldSpecs.Count()}"
            };
        }

        public List<string> AssemblyReport()
        {
            var groups = _specManager.Assemblies.Values.Where(spec => spec != AssemblySpec.NullSpec && !spec.Skipped && spec.Analysed)
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
