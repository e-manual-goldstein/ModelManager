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
    public class DotNetFrameworkLoader
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

        public DotNetFrameworkLoader(string imageRuntimeVersion) 
        {
            _imageRuntimeVersion = imageRuntimeVersion;
            CreateLoadContext(GetFilePathsForLoadContext());
        }

        private void CreateLoadContext(List<string> filePaths)
        {
            if (_loadContext != null)
            {
                _loadContext.Dispose();                
            }
            _resolver = new PathAssemblyResolver(filePaths);
            _loadContext = new MetadataLoadContext(_resolver);
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

        public bool TryLoadAssembly(string filePath, out Assembly assembly)
        {
            bool success = false;
            assembly = null;            
            try
            {
                CreateLoadContext(GetFilePathsForLoadContext());                
                assembly = _loadContext.LoadFromAssemblyPath(filePath);
            }
            catch
            {

            }
            return success;
        }

        public bool TryLoadReferencedAssemblies(string filePath, out IEnumerable<Assembly> assemblies)
        {
            CreateLoadContext(GetFilePathsForLoadContext(GetReferencedAssemblyLocations(filePath)));
            var assemblyNames = GetAssemblyNamesForAssembly(filePath);            
            return TryLoadReferences(assemblyNames, out assemblies);
        }

        private string[] GetAssemblyNamesForAssembly(string filePath)
        {
            try
            {
                var assembly = _loadContext.LoadFromAssemblyPath(filePath);
                return assembly.GetReferencedAssemblies().Select(c => c.FullName).ToArray();
            }
            catch (Exception ex)
            {
                Faults.Add($"Assembly: {filePath} {ex.Message}");
            }            
            return Array.Empty<string>();
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
