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
    public abstract class AssemblyLoader
    {
        protected List<string> _filePathsForLoadContext = new List<string>();

        public static AssemblyLoader GetLoader(string targetFrameworkVersion, string imageRuntimeVersion)
        {
            if (string.IsNullOrEmpty(targetFrameworkVersion) && string.IsNullOrEmpty(imageRuntimeVersion))
            {
                return CreateOrGetLoader("v4.0.30319");
            }
            if (!string.IsNullOrEmpty(targetFrameworkVersion))
            {
                return GetLoaderForFrameworkVersion(targetFrameworkVersion);
            }
            if (!string.IsNullOrEmpty(imageRuntimeVersion))
            {
                return GetLoaderForImageRuntimeVersion(imageRuntimeVersion);
            }
            return new DotNetFrameworkLoader("v4.0.30319");
        }

        private static AssemblyLoader GetLoaderForFrameworkVersion(string targetFrameworkVersion)
        {
            return new DotNetFrameworkLoader("v4.0.30319");            
        }

        private static AssemblyLoader GetLoaderForImageRuntimeVersion(string targetFrameworkVersion)
        {
            return new DotNetFrameworkLoader("v4.0.30319");
        }

        private static AssemblyLoader CreateOrGetLoader(string imageRuntimeVersion)
        {
            if (!_loaderCache.TryGetValue(imageRuntimeVersion, out AssemblyLoader loader))
            {
                _loaderCache.Add(imageRuntimeVersion, new DotNetFrameworkLoader(imageRuntimeVersion));
            }
            return _loaderCache[imageRuntimeVersion];
        }

        private static Dictionary<string, AssemblyLoader> _loaderCache = new Dictionary<string, AssemblyLoader>();

        public abstract Assembly LoadAssemblyByName(string assemblyName);

        public abstract Assembly LoadAssemblyByPath(string assemblyPath);

        public abstract IEnumerable<Assembly> LoadReferencedAssembliesByRootPath(string rootAssemblyPath);

        internal void AddDirectory(string directory)
        {
            _filePathsForLoadContext.AddRange(Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories));
        }
    }
}
