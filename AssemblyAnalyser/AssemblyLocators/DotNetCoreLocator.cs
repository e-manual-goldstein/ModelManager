using AssemblyAnalyser;
using AssemblyAnalyser.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssemblyAnalyser
{
    public class DotNetCoreLocator : AssemblyLocator
    {
        string _imageRuntimeVersion;

        AssemblyPathCache _assemblyPathCache;

        string _baseDotNetPath = "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\";
        
        public DotNetCoreLocator(string imageRuntimeVersion) : base()
        {
            _imageRuntimeVersion = imageRuntimeVersion;
            _assemblyPathCache = AssemblyPathCache.LoadPathCache($"AssemblyLookup_{imageRuntimeVersion}.txt");
            LoadFilePaths(GetBaseFilePathsForLocator());
        }

        public DotNetCoreLocator() : base()
        {
            throw new NotImplementedException();
        }

        protected override List<string> GetBaseFilePathsForLocator()
        {
            var paths = new List<string>();
            
            var frameworkDllPaths = Directory.GetFiles(_baseDotNetPath, "*.dll", SearchOption.AllDirectories);

            paths.AddRange(frameworkDllPaths.Where(path => !path.Contains("Temporary ASP.NET Files", StringComparison.CurrentCultureIgnoreCase)));

            return paths;
        }

        public override string LocateAssemblyByName(string assemblyName)
        {
            if (!_locatedAssembliesByName.TryGetValue(assemblyName, out string assemblyPath))
            {
                if (!TryLocateAssemblyByName(assemblyName, out assemblyPath))
                {                   
                    Faults.Add($"Assembly Not Found: {assemblyName}");                    
                }
                AddFilePathToCache(assemblyName, assemblyPath);
            }
            return assemblyPath;
        }

        private void AddFilePathToCache(string assemblyName, string assemblyPath)
        {
            _locatedAssembliesByName[assemblyName] = assemblyPath;
            _assemblyPathCache.Add(assemblyName, assemblyPath);
        }

        private bool TryLocateAssemblyByName(string assemblyName, out string assembly)
        {
            bool success = true;
            assembly = null;
            try
            {
                var assemblyFileName = $"{assemblyName.ParseShortName()}.dll";
                var matches = _filePathsForLocator.Where(r => Path.GetFileName(r).Equals(assemblyFileName, StringComparison.CurrentCultureIgnoreCase));
                if (!matches.Any())
                {
                    if (!_assemblyPathCache.FilePaths.TryGetValue(assemblyName, out assembly))
                    {
                        success = SystemAssemblyLookup.TryGetPath(assemblyName, out assembly);
                    }
                }
                else if (matches.Count() == 1)
                {
                    assembly = matches.Single();
                }
                else
                {
                    Faults.Add($"Assembly found in more than one location: {assemblyName}");
                    assembly = matches.First();                    
                }
                success = !string.IsNullOrEmpty(assembly);
            }
            catch (Exception ex)
            {
                Faults.Add($"Assembly: {assemblyName} {ex.Message}");
                success = false;                
            }
            return success;
        }

        private bool TryLocateAssemblies(IEnumerable<string> assemblyNames, out IEnumerable<string> assemblies)
        {
            var list = new List<string>();
            bool success = true;
            foreach (var assemblyName in assemblyNames)
            {
                var assembly = LocateAssemblyByName(assemblyName);
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

        class AssemblyPathCache
        {
            public AssemblyPathCache()
            {

            }

            public AssemblyPathCache(string fileName)
            {
                FileName = fileName;
            }

            public string FileName { get; set; }
            public ConcurrentDictionary<string, string> FilePaths { get; set; } = new();
            
            public static AssemblyPathCache LoadPathCache(string fileName)
            {
                var cacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                if (File.Exists(cacheFilePath))
                {
                    return JsonConvert.DeserializeObject<AssemblyPathCache>(File.ReadAllText(cacheFilePath));
                }
                return new AssemblyPathCache(fileName);
            }

            public void SaveCache()
            {
                var cacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
                var savedCache = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(cacheFilePath, savedCache);                
            }

            static object _lock = new object();

            internal void Add(string assemblyName, string assemblyPath)
            {
                if (FilePaths.TryAdd(assemblyName, assemblyPath));
                lock (_lock)
                {
                    SaveCache();
                }
                
            }
        }
    }
}
