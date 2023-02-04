using AssemblyAnalyser.AssemblyLoaders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class DotNetFrameworkLoader : AssemblyLoader
    {
        List<string> _filePathsForLoadContext;
        string _imageRuntimeVersion;
        PathAssemblyResolver _resolver;
        MetadataLoadContext _loadContext;

        Dictionary<string, string[]> _baseFrameworkPaths = new Dictionary<string, string[]>() 
        {
            { "v1.0.3705",  new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v2.0.50727", "C:\\Windows\\Microsoft.NET\\Framework\\v2.0.50727"} },
            { "v1.1.4322",  new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v2.0.50727", "C:\\Windows\\Microsoft.NET\\Framework\\v2.0.50727" } },
            { "v2.0.50727", new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v2.0.50727", "C:\\Windows\\Microsoft.NET\\Framework\\v2.0.50727" } },
            { "v4.0.30319", new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319", "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319" } },
            { "COMPLUS",    new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319", "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319" } }
        };

        Dictionary<string, Assembly> _loadedAssembliesByName = new Dictionary<string, Assembly>();
        Dictionary<string, Assembly> _loadedAssembliesByPath = new Dictionary<string, Assembly>();

        public DotNetFrameworkLoader(string imageRuntimeVersion, IEnumerable<string> additionalPaths) : this(imageRuntimeVersion)
        {
            CreateLoadContext(GetFilePathsForLoadContext().Concat(additionalPaths));
        }

        private DotNetFrameworkLoader(string imageRuntimeVersion)
        {
            _imageRuntimeVersion = imageRuntimeVersion;
        }

        private void CreateLoadContext(IEnumerable<string> filePaths)
        {
            if (_loadContext != null)
            {
                _loadContext.Dispose();                
            }
            DropCache();
            _resolver = new PathAssemblyResolver(filePaths);
            _loadContext = new MetadataLoadContext(_resolver);
        }

        private void DropCache()
        {
            _loadedAssembliesByPath.Clear();
            _loadedAssembliesByPath.Clear();
            
        }

        private List<string> GetFilePathsForLoadContext(IEnumerable<string> additionalPaths = default)
        {
            additionalPaths ??= Enumerable.Empty<string>();
            var paths = new List<string>(additionalPaths);
            string fxPath = string.Empty;
            var basePaths = _baseFrameworkPaths[_imageRuntimeVersion];
            if (basePaths.Length > 1)
            {
                var version = IntPtr.Size == 8 ? "Framework64" : "Framework64";
                fxPath = basePaths.SingleOrDefault(p => p.StartsWith($"C:\\Windows\\Microsoft.NET\\{version}"));
            }
            else
            {
                fxPath = basePaths[0];
            }

            var frameworkDllPaths = Directory.GetFiles(fxPath, "*.dll", SearchOption.AllDirectories);
            
            paths.AddRange(frameworkDllPaths);

            return paths;
        }

        public List<string> Faults { get; } = new List<string>();
        public List<string> Results { get; } = new List<string>();

        public override Assembly LoadAssemblyByName(string assemblyName)
        {
            if (!_loadedAssembliesByName.TryGetValue(assemblyName, out Assembly assembly))
            {
                if (!TryLoadAssemblyByName(assemblyName, out assembly))
                {
                    if (SystemAssemblyLookup.TryGetPath(assemblyName, out var assemblyPath))
                    {
                        TryLoadAssemblyByPath(assemblyPath, out assembly);
                    }
                }                
            }
            return assembly;
        }

        public override Assembly LoadAssemblyByPath(string assemblyPath)
        {
            if (!_loadedAssembliesByPath.TryGetValue(assemblyPath, out Assembly assembly))
            {
                TryLoadAssemblyByPath(assemblyPath, out assembly);
            }
            return assembly;
        }

        public override IEnumerable<Assembly> LoadReferencedAssembliesByRootPath(string rootAssemblyPath)
        {
            if (!_loadedAssembliesByPath.TryGetValue(rootAssemblyPath, out Assembly assembly))
            {
                TryLoadAssemblyByPath(rootAssemblyPath, out assembly);
            }
            if (assembly == null)
            {
                return Array.Empty<Assembly>();
            }
            TryLoadReferencedAssemblies(rootAssemblyPath, out var assemblies);
            assemblies ??= Array.Empty<Assembly>();
            return assemblies;
        }

        private bool TryLoadAssemblyByPath(string filePath, out Assembly assembly)
        {
            bool success = true;
            assembly = null;            
            try
            {
                CreateLoadContext(GetFilePathsForLoadContext());
                assembly = _loadContext.LoadFromAssemblyPath(filePath);
                CacheLoadedAssembly(assembly);
            }
            catch (Exception ex)
            {
                if (!_loadContext.GetAssemblies().Any(d => d.Location == filePath))
                {
                    Faults.Add($"Assembly: {filePath} {ex.Message}");
                    success = false;
                }
                assembly = _loadContext.GetAssemblies().SingleOrDefault(d => d.Location == filePath);
            }
            return success;
        }

        private bool TryLoadAssemblyByName(string assemblyName, out Assembly assembly)
        {
            bool success = true;
            assembly = null;
            try
            {
                CreateLoadContext(GetFilePathsForLoadContext());
                assembly = _loadContext.LoadFromAssemblyName(assemblyName);
                CacheLoadedAssembly(assembly);                
            }            
            catch (Exception ex)
            {
                if (!_loadContext.GetAssemblies().Any(d => d.FullName == assemblyName))
                {
                    Faults.Add($"Assembly: {assemblyName} {ex.Message}");
                    success = false;
                }
                assembly = _loadContext.GetAssemblies().SingleOrDefault(d => d.FullName == assemblyName);
            }
            return success;
        }

        private bool TryLoadFromGAC(string assemblyName, out Assembly assembly)
        {
            throw new NotImplementedException();
        }

        private void CacheLoadedAssembly(Assembly assembly)
        {
            _loadedAssembliesByPath[assembly.Location] = assembly;
            _loadedAssembliesByPath[assembly.FullName] = assembly;
        }

        private bool TryLoadReferencedAssemblies(string filePath, out IEnumerable<Assembly> assemblies)
        {
            CreateLoadContext(GetFilePathsForLoadContext(GetReferencedAssemblyLocations(filePath)));
            var assemblyNames = GetAssemblyNamesForAssembly(filePath);
            return TryLoadReferences(assemblyNames, out assemblies);
        }

        private IEnumerable<string> GetAssemblyNamesForAssembly(string filePath)
        {
            var assembly = LoadAssemblyByPath(filePath);
            return assembly != null ? assembly.GetReferencedAssemblies().Select(d => d.FullName) : Enumerable.Empty<string>();
        }

        private bool TryLoadReferences(IEnumerable<string> assemblyNames, out IEnumerable<Assembly> assemblies)
        {
            //assemblies = new List<Assembly>();
            bool success = true;
            foreach (var assemblyName in assemblyNames)
            {
                try
                {
                    var referencedAssembly = _loadContext.LoadFromAssemblyName(assemblyName);
                    Results.Add($"{assemblyName}");                    
                }
                catch (Exception ex)
                {
                    if (!_loadContext.GetAssemblies().Any(d => d.FullName == assemblyName))
                    {
                        Faults.Add($"Assembly: {assemblyName} {ex.Message}");
                        success = false;
                    }
                }                
            }
            assemblies = _loadContext.GetAssemblies().Where(a => assemblyNames.Contains(a.FullName));
            return success;
        }

        private IEnumerable<string> GetReferencedAssemblyLocations(string assemblyPath)
        {
            return Directory.GetFiles(Path.GetDirectoryName(assemblyPath), "*.dll", SearchOption.AllDirectories);
        }
    }
}
