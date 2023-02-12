using AssemblyAnalyser.AssemblyLoaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AssemblyAnalyser
{
    public class DotNetFrameworkLoader : AssemblyLoader
    {
        string _imageRuntimeVersion;
        //PathAssemblyResolver _resolver;

        Dictionary<string, string[]> _baseFrameworkPaths = new Dictionary<string, string[]>()
        {
            { "v1.0.3705",  new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v2.0.50727", "C:\\Windows\\Microsoft.NET\\Framework\\v2.0.50727"} },
            { "v1.1.4322",  new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v2.0.50727", "C:\\Windows\\Microsoft.NET\\Framework\\v2.0.50727" } },
            { "v2.0.50727", new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v2.0.50727", "C:\\Windows\\Microsoft.NET\\Framework\\v2.0.50727" } },
            { "v4.0.30319", new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319", "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319" } },
            { "COMPLUS",    new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319", "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319" } }
        };

        public DotNetFrameworkLoader(string imageRuntimeVersion) : base()
        {
            _imageRuntimeVersion = imageRuntimeVersion;
            CreateLoadContext(GetBaseFilePathsForLoadContext());
        }

        public DotNetFrameworkLoader() : base()
        {
            //CreateLoadContext(GetBaseFilePathsForLoadContext());
            throw new NotImplementedException();
        }

        protected override void CreateLoadContext(IEnumerable<string> filePaths)
        {
            _filePathsForLoadContext.AddRange(filePaths);
            Reset();
        }

        protected override List<string> GetBaseFilePathsForLoadContext()
        {
            var paths = new List<string>();
            string fxPath = string.Empty;
            var basePaths = _baseFrameworkPaths[_imageRuntimeVersion];
            if (basePaths.Length > 1)
            {
                var version = IntPtr.Size == 8 ? "Framework64" : "Framework64";
                fxPath = basePaths.SingleOrDefault(p => p.StartsWith($"{BASE_FRAMEWORK_PATH}{version}"));
            }
            else
            {
                fxPath = basePaths[0];
            }

            var frameworkDllPaths = Directory.GetFiles(fxPath, "*.dll", SearchOption.AllDirectories);

            paths.AddRange(frameworkDllPaths.Where(path => !path.Contains("Temporary ASP.NET Files", StringComparison.CurrentCultureIgnoreCase)));

            return paths;
        }

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

        public override IEnumerable<string> PreLoadReferencedAssembliesByRootPath(string rootAssemblyPath)
        {
            if (!_loadedAssembliesByPath.TryGetValue(rootAssemblyPath, out Assembly assembly))
            {
                TryLoadAssemblyByPath(rootAssemblyPath, out assembly);
            }
            if (assembly == null)
            {
                return Array.Empty<string>();
            }
            TryPreLoadReferencedAssemblies(rootAssemblyPath, out var assemblyPaths);
            assemblyPaths ??= Array.Empty<string>();
            return assemblyPaths;
        }

        private bool TryLoadAssemblyByPath(string filePath, out Assembly assembly)
        {
            bool success = true;
            assembly = null;
            try
            {
                InitialiseLoadContext(filePath);
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
                //CreateLoadContext(GetBaseFilePathsForLoadContext());
                assembly = _loadContext.LoadFromAssemblyName(assemblyName);
                CacheLoadedAssembly(assembly);
            }
            catch (Exception ex)
            {
                if (!_loadContext.GetAssemblies().Any(d => d.FullName == assemblyName))
                {
                    if (!SystemAssemblyLookup.TryGetPath(assemblyName, out var assemblyPath)
                        || !TryLoadAssemblyByPath(assemblyPath, out assembly))
                    {

                        Faults.Add($"Assembly: {assemblyName} {ex.Message}");
                        success = false;
                    }
                }
                assembly = _loadContext.GetAssemblies().SingleOrDefault(d => d.FullName == assemblyName);
            }
            return success;
        }

        private bool TryLoadReferencedAssemblies(string filePath, out IEnumerable<Assembly> assemblies)
        {
            InitialiseLoadContext(Directory.GetFiles(Path.GetDirectoryName(filePath), "*dll"));
            var assemblyNames = GetAssemblyNamesForAssembly(filePath);
            return TryLoadReferences(assemblyNames, out assemblies);
        }

        private bool TryPreLoadReferencedAssemblies(string filePath, out IEnumerable<string> assemblyPaths)
        {
            InitialiseLoadContext(Directory.GetFiles(Path.GetDirectoryName(filePath), "*dll"));
            var assemblyNames = GetAssemblyNamesForAssembly(filePath);
            return TryPreLoadReferences(assemblyNames, out assemblyPaths);
        }

        private IEnumerable<string> GetAssemblyNamesForAssembly(string filePath)
        {
            var assembly = LoadAssemblyByPath(filePath);
            return assembly != null ? assembly.GetReferencedAssemblies().Select(d => d.FullName) : Enumerable.Empty<string>();
        }

        private bool TryLoadReferences(IEnumerable<string> assemblyNames, out IEnumerable<Assembly> assemblies)
        {
            var list = new List<Assembly>();
            bool success = true;
            foreach (var assemblyName in assemblyNames)
            {
                var assembly = LoadAssemblyByName(assemblyName);
                if (assembly == null)
                {
                    success = false;
                }
                else
                {
                    list.Add(assembly);
                }
            }
            assemblies = list;
            return success;
        }

        private bool TryPreLoadReferences(IEnumerable<string> assemblyNames, out IEnumerable<string> assemblies)
        {
            var list = new List<string>();
            bool success = true;
            foreach (var assemblyName in assemblyNames)
            {
                var assembly = LoadAssemblyByName(assemblyName);
                if (assembly == null)
                {
                    success = false;
                }
                else
                {
                    list.Add(assembly.FullName);
                }
            }
            assemblies = list;
            return success;
        }


    }
}
