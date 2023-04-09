using AssemblyAnalyser.Extensions;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
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
            if (!string.IsNullOrEmpty(targetFrameworkVersion))
            {
                return GetLocatorForFrameworkVersion(targetFrameworkVersion);
            }
            if (!string.IsNullOrEmpty(imageRuntimeVersion))
            {
                return GetLocatorForImageRuntimeVersion(imageRuntimeVersion);
            }
            return CreateOrGetLocatorForRuntimeVersion("v4.0.30319");
        }

        public static AssemblyLocator GetLocator(ModuleDefinition module)
        {

            var targetFrameworkAttribute = module.GetCustomAttributes()
                .SingleOrDefault(t => t.AttributeType.FullName == typeof(TargetFrameworkAttribute).FullName);
            if (targetFrameworkAttribute != null)
            {
                var frameworkVersion = $"{targetFrameworkAttribute.ConstructorArguments[0].Value}";
                if (frameworkVersion.StartsWith(".NETCoreApp"))
                {
                    var locator = CreateOrGetDotNetCoreLocatorForVersion(frameworkVersion);
                    locator.AddDirectory(Path.GetDirectoryName(module.FileName));
                    return locator;
                }
                else if (frameworkVersion.StartsWith(".NETStandard"))
                {
                    var locator = CreateOrGetDotNetStandardLocatorForVersion(frameworkVersion);
                    locator.AddDirectory(Path.GetDirectoryName(module.FileName));
                    return locator;
                }
            }
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
            return _runtimeImageCache.GetOrAdd(imageRuntimeVersion, (imageRuntimeVersion) => new DotNetFrameworkLocator(imageRuntimeVersion));            
        }

        private static AssemblyLocator CreateOrGetLocatorForFrameworkVersion(string targetFrameworkVersion)
        {
            return _targetFrameworkCache.GetOrAdd(targetFrameworkVersion, (imageRuntimeVersion) => new DotNetFrameworkLocator(imageRuntimeVersion));
        }

        private static AssemblyLocator CreateOrGetDotNetCoreLocatorForVersion(string targetFrameworkVersion)
        {
            return _targetFrameworkCache.GetOrAdd(targetFrameworkVersion, (targetFrameworkVersion) => new DotNetCoreLocator(targetFrameworkVersion));
        }

        private static AssemblyLocator CreateOrGetDotNetStandardLocatorForVersion(string targetFrameworkVersion)
        {
            return _targetFrameworkCache.GetOrAdd(targetFrameworkVersion, (targetFrameworkVersion) => new DotNetStandardLocator(targetFrameworkVersion));
        }

        protected ConcurrentDictionary<string, string> _locatedAssembliesByName = new ConcurrentDictionary<string, string>();

        private static ConcurrentDictionary<string, AssemblyLocator> _runtimeImageCache = new ConcurrentDictionary<string, AssemblyLocator>();
        private static ConcurrentDictionary<string, AssemblyLocator> _targetFrameworkCache = new ConcurrentDictionary<string, AssemblyLocator>();

        public abstract string LocateAssemblyByName(string assemblyName);
        
        protected void LoadFilePaths(IEnumerable<string> filePaths)
        {
            _filePathsForLocator = _filePathsForLocator.Union(filePaths).Distinct().ToList();
        }

        ConcurrentDictionary<string, string[]> _addedDirectories = new ConcurrentDictionary<string, string[]>();

        internal void AddDirectory(string directory)
        {
            string[] getFiles(string dir)
            {
                return Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories);
            }
            var filePaths = _addedDirectories.GetOrAdd(directory, getFiles);
            LoadFilePaths(filePaths);
        }

        public static bool IsSystemAssembly(string assemblyLocation)
        {
            return assemblyLocation.StartsWith(BASE_FRAMEWORK_PATH, StringComparison.CurrentCultureIgnoreCase) 
                || assemblyLocation.StartsWith(GLOBAL_ASSEMBLY_CACHE_PATH, StringComparison.CurrentCultureIgnoreCase);
        }

    }
}
