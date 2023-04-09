using AssemblyAnalyser;
using AssemblyAnalyser.AssemblyLocators;
using AssemblyAnalyser.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AssemblyAnalyser
{
    public class DotNetCoreLocator : AssemblyLocator
    {
        string _targetVersion;
        string _bestAvailableVersion;

        AssemblyPathCache _assemblyPathCache;

        string _baseDotNetPath = "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\";
        
        public DotNetCoreLocator(string targetVersion) : base()
        {
            _targetVersion = targetVersion;
            _bestAvailableVersion = DetermineBestAvailableVersion(targetVersion);
            _assemblyPathCache = AssemblyPathCache.LoadPathCache($"AssemblyLookup_{targetVersion}.txt");
            LoadFilePaths(GetBaseFilePathsForLocator());
        }

        public string DotNetVersion => _bestAvailableVersion;

        private string DetermineBestAvailableVersion(string fullVersionName)
        {
            var targetVersion = Regex.Match(fullVersionName, "\\.NETCoreApp,Version=v(?'SemVer'.*)").Groups["SemVer"].Value;
            var installedVersions = Directory.EnumerateDirectories(_baseDotNetPath)
                .Select(d => d.Replace(_baseDotNetPath, ""));
            return installedVersions.Contains(targetVersion) ? targetVersion : 
                VersionPicker.PickBestVersion(installedVersions.ToArray(), targetVersion);
        }

        protected override List<string> GetBaseFilePathsForLocator()
        {
            var paths = new List<string>();
            paths.AddRange(Directory.GetFiles(Path.Combine(_baseDotNetPath, _bestAvailableVersion), "*.dll", SearchOption.AllDirectories));            
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
