﻿using AssemblyAnalyser;
using AssemblyAnalyser.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssemblyAnalyser
{
    public class DotNetFrameworkLocator : AssemblyLocator
    {
        string _imageRuntimeVersion;

        Dictionary<string, string[]> _baseFrameworkPaths = new Dictionary<string, string[]>()
        {
            { "v1.0.3705",  new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v2.0.50727", "C:\\Windows\\Microsoft.NET\\Framework\\v2.0.50727"} },
            { "v1.1.4322",  new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v2.0.50727", "C:\\Windows\\Microsoft.NET\\Framework\\v2.0.50727" } },
            { "v2.0.50727", new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v2.0.50727", "C:\\Windows\\Microsoft.NET\\Framework\\v2.0.50727" } },
            { "v4.0.30319", new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319", "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319" } },
            { "COMPLUS",    new [] { "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319", "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319" } }
        };

        public DotNetFrameworkLocator(string imageRuntimeVersion) : base()
        {
            _imageRuntimeVersion = imageRuntimeVersion;            
            LoadFilePaths(GetBaseFilePathsForLocator());
        }

        public DotNetFrameworkLocator() : base()
        {
            throw new NotImplementedException();
        }



        protected override List<string> GetBaseFilePathsForLocator()
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

        public override string LocateAssemblyByName(string assemblyName)
        {
            if (!_locatedAssembliesByName.TryGetValue(assemblyName, out string assemblyPath))
            {
                if (!TryLocateAssemblyByName(assemblyName, out assemblyPath))
                {                   
                    Faults.Add($"Assembly Not Found: {assemblyName}");                    
                }
                else
                {
                    _locatedAssembliesByName[assemblyName] = assemblyPath;
                }
            }
            return assemblyPath;
        }

        private bool TryLocateAssemblyByName(string assemblyName, out string assembly)
        {
            bool success = true;
            assembly = null;
            try
            {
                var assemblyFileName = $"{assemblyName.ParseShortName()}.dll";
                var matches = _filePathsForLocator.Where(r => Path.GetFileName(r) == assemblyFileName);
                if (!matches.Any())
                {
                    success = SystemAssemblyLookup.TryGetPath(assemblyName, out assembly);
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
                success = assembly != null;
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
    }
}