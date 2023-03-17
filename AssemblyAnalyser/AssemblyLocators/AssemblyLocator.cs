using AssemblyAnalyser.Extensions;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public abstract class AssemblyLocator
    {
        protected List<string> _filePathsForLocator = new List<string>();
        protected const string BASE_FRAMEWORK_PATH = "C:\\Windows\\Microsoft.NET\\";
        protected const string GLOBAL_ASSEMBLY_CACHE_PATH = "C:\\Windows\\assembly";

        public AssemblyLocator()
        {            
        }

        public List<string> Faults { get; } = new List<string>();
        public List<string> Results { get; } = new List<string>();

        protected abstract List<string> GetBaseFilePathsForLocator();

        public static AssemblyLocator GetLocator(string targetFrameworkVersion, string imageRuntimeVersion)
        {
            if (string.IsNullOrEmpty(targetFrameworkVersion) && string.IsNullOrEmpty(imageRuntimeVersion))
            {
                return CreateOrGetLocatorForRuntimeVersion("v4.0.30319");
            }
            if (!string.IsNullOrEmpty(imageRuntimeVersion))
            {
                return GetLocatorForImageRuntimeVersion(imageRuntimeVersion);
            }
            if (!string.IsNullOrEmpty(targetFrameworkVersion))
            {
                return GetLocatorForFrameworkVersion(targetFrameworkVersion);
            }
            return CreateOrGetLocatorForRuntimeVersion("v4.0.30319");
        }

        public static AssemblyLocator GetLocator(ModuleDefinition module)
        {
            if (!string.IsNullOrEmpty(module.RuntimeVersion))
            {
                var locator = CreateOrGetLocatorForRuntimeVersion(module.RuntimeVersion);
                locator.AddDirectory(Path.GetDirectoryName(module.FileName));
                return locator;
            }
            throw new NotImplementedException();
        }

        private static AssemblyLocator GetLocatorForFrameworkVersion(string targetFrameworkVersion)
        {
            return CreateOrGetLocatorForFrameworkVersion(targetFrameworkVersion);
            //return new DotNetFrameworkLoader("v4.0.30319");            
        }

        private static AssemblyLocator GetLocatorForImageRuntimeVersion(string imageRuntimeVersion)
        {
            return CreateOrGetLocatorForRuntimeVersion(imageRuntimeVersion);
            //return new DotNetFrameworkLoader("v4.0.30319");
        }

        private static AssemblyLocator CreateOrGetLocatorForRuntimeVersion(string imageRuntimeVersion)
        {
            if (!_runtimeImageCache.TryGetValue(imageRuntimeVersion, out AssemblyLocator loader))
            {
                loader = new DotNetFrameworkLocator(imageRuntimeVersion);
                _runtimeImageCache.Add(imageRuntimeVersion, loader);                
            }
            return _runtimeImageCache[imageRuntimeVersion];
        }

        private static AssemblyLocator CreateOrGetLocatorForFrameworkVersion(string targetFrameworkVersion)
        {
            if (!_targetFrameworkCache.TryGetValue(targetFrameworkVersion, out AssemblyLocator loader))
            {
                loader = new DotNetFrameworkLocator();
                _targetFrameworkCache.Add(targetFrameworkVersion, loader);                
            }
            return _targetFrameworkCache[targetFrameworkVersion];
        }

        protected Dictionary<string, string> _locatedAssembliesByName = new Dictionary<string, string>();

        private static Dictionary<string, AssemblyLocator> _runtimeImageCache = new Dictionary<string, AssemblyLocator>();
        private static Dictionary<string, AssemblyLocator> _targetFrameworkCache = new Dictionary<string, AssemblyLocator>();

        private static void AddAssemblyLocatorAsKnownHandler(object sender, Assembly assembly)
        {
            if (sender is AssemblyLocator assemblyLoader)
            {
                if (!_runtimeImageCache.ContainsKey(assembly.ImageRuntimeVersion))
                {
                    _runtimeImageCache[assembly.ImageRuntimeVersion] = assemblyLoader;
                }
                if (assembly.TryGetTargetFrameworkVersion(out string targetFrameworkVersion)
                    && !_targetFrameworkCache.ContainsKey(targetFrameworkVersion)) 
                { 
                    _targetFrameworkCache.Add(targetFrameworkVersion, assemblyLoader);
                }
            }
        }

        public abstract string LocateAssemblyByName(string assemblyName);
        
        protected void LoadFilePaths(IEnumerable<string> filePaths)
        {
            _filePathsForLocator = _filePathsForLocator.Union(filePaths).Distinct().ToList();
        }

        internal void AddDirectory(string directory)
        {
            LoadFilePaths(Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories));
        }

        public static bool IsSystemAssembly(string assemblyLocation)
        {
            return assemblyLocation.StartsWith(BASE_FRAMEWORK_PATH, StringComparison.CurrentCultureIgnoreCase) 
                || assemblyLocation.StartsWith(GLOBAL_ASSEMBLY_CACHE_PATH, StringComparison.CurrentCultureIgnoreCase);
        }

    }
}
