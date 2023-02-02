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
        readonly ISpecManager _specManager;
        private IExceptionManager _exceptionManager;
        private bool _disposed;
        
        public Analyser(string workingDirectory, ILogger logger, ISpecManager specManager, IExceptionManager exceptionManager) 
        {
            _specManager = specManager;
            _exceptionManager = exceptionManager;
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
                //_specManager.LoadAssemblyFromPath(filePath, out Assembly assembly);
                //var assemblySpec = _specManager.LoadAssemblySpec(assembly);
                //assemblySpec.Process(_specManager);
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

        public void BuildAssemblies(IEnumerable<string> assemblyNames)
        {
            foreach (var assemblyName in assemblyNames)
            {
                var assembly = _specManager.Assemblies[assemblyName];
                assembly.Process();
            }
        }

        #endregion

        #region Type Specs

        public void BuildTypes()
        {
            foreach (var typeSpec in _specManager.Types.Values)
            {
                typeSpec.Process();
            }
        }

        #endregion

        public List<string> Report()
        {
            return new List<string>() 
            {  
                $"Assemblies: {_specManager.Assemblies.Count()}",
                $"Types: {_specManager.Types.Count()}",
                $"Properties {_specManager.Properties.Count()}",
                $"Methods {_specManager.Methods.Count()}",
                $"Fields {_specManager.Fields.Count()}"
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

        public List<string> MissingFileReport()
        {
            return _exceptionManager.MissingFiles;
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
                //_exceptionManager.Handle(fileNotFoundException);
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
