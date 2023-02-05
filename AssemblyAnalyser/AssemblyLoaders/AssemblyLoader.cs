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
        protected MetadataLoadContext _loadContext;
        
        public AssemblyLoader()
        {            
        }

        protected abstract void CreateLoadContext(IEnumerable<string> filePaths);

        protected void InitialiseLoadContext(string filePath)
        {
            if (!_filePathsForLoadContext.Contains(filePath))
            {
                _filePathsForLoadContext.Add(filePath);
                Reset();
            }
        }

        protected void InitialiseLoadContext(IEnumerable<string> filePaths)
        {
            foreach (string filePath in filePaths)
            {
                if (!_filePathsForLoadContext.Contains(filePath))
                {
                    _filePathsForLoadContext.Add(filePath);
                }
            }
            Reset();
        }

        public List<string> Faults { get; } = new List<string>();
        public List<string> Results { get; } = new List<string>();

        protected abstract List<string> GetBaseFilePathsForLoadContext();

        public static AssemblyLoader GetLoader(string targetFrameworkVersion, string imageRuntimeVersion)
        {
            if (string.IsNullOrEmpty(targetFrameworkVersion) && string.IsNullOrEmpty(imageRuntimeVersion))
            {
                return CreateOrGetLoaderForRuntimeVersion("v4.0.30319");
            }
            if (!string.IsNullOrEmpty(targetFrameworkVersion))
            {
                return GetLoaderForFrameworkVersion(targetFrameworkVersion);
            }
            if (!string.IsNullOrEmpty(imageRuntimeVersion))
            {
                return GetLoaderForImageRuntimeVersion(imageRuntimeVersion);
            }
            return CreateOrGetLoaderForRuntimeVersion("v4.0.30319");
        }

        private static AssemblyLoader GetLoaderForFrameworkVersion(string targetFrameworkVersion)
        {
            return CreateOrGetLoaderForFrameworkVersion(targetFrameworkVersion);
            //return new DotNetFrameworkLoader("v4.0.30319");            
        }

        private static AssemblyLoader GetLoaderForImageRuntimeVersion(string imageRuntimeVersion)
        {
            return CreateOrGetLoaderForRuntimeVersion(imageRuntimeVersion);
            //return new DotNetFrameworkLoader("v4.0.30319");
        }

        private static AssemblyLoader CreateOrGetLoaderForRuntimeVersion(string imageRuntimeVersion)
        {
            if (!_runtimeImageCache.TryGetValue(imageRuntimeVersion, out AssemblyLoader loader))
            {
                loader = new DotNetFrameworkLoader(imageRuntimeVersion);
                _runtimeImageCache.Add(imageRuntimeVersion, loader);
                loader.OnAssemblySuccessfullyLoaded += AddAssemblyLoaderAsKnownHandler;
            }
            return _runtimeImageCache[imageRuntimeVersion];
        }

        private static AssemblyLoader CreateOrGetLoaderForFrameworkVersion(string targetFrameworkVersion)
        {
            if (!_targetFrameworkCache.TryGetValue(targetFrameworkVersion, out AssemblyLoader loader))
            {
                loader = new DotNetFrameworkLoader(targetFrameworkVersion);
                _targetFrameworkCache.Add(targetFrameworkVersion, loader);
                loader.OnAssemblySuccessfullyLoaded += AddAssemblyLoaderAsKnownHandler;
            }
            return _targetFrameworkCache[targetFrameworkVersion];
        }

        private static Dictionary<string, AssemblyLoader> _runtimeImageCache = new Dictionary<string, AssemblyLoader>();
        private static Dictionary<string, AssemblyLoader> _targetFrameworkCache = new Dictionary<string, AssemblyLoader>();

        private static void AddAssemblyLoaderAsKnownHandler(object sender, Assembly assembly)
        {
            if (sender is AssemblyLoader assemblyLoader)
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

        public abstract Assembly LoadAssemblyByName(string assemblyName);

        public abstract Assembly LoadAssemblyByPath(string assemblyPath);

        public abstract IEnumerable<Assembly> LoadReferencedAssembliesByRootPath(string rootAssemblyPath);

        public abstract IEnumerable<string> PreLoadReferencedAssembliesByRootPath(string rootAssemblyPath);

        internal void AddDirectory(string directory)
        {
            _filePathsForLoadContext.AddRange(Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories));
        }

        protected Dictionary<string, Assembly> _loadedAssembliesByName = new Dictionary<string, Assembly>();
        protected Dictionary<string, Assembly> _loadedAssembliesByPath = new Dictionary<string, Assembly>();

        protected void CacheLoadedAssembly(Assembly assembly)
        {
            OnAssemblySuccessfullyLoaded(this, assembly);
            _loadedAssembliesByPath[assembly.Location] = assembly;
            _loadedAssembliesByName[assembly.FullName] = assembly;
        }

        protected void DropCache()
        {
            _loadedAssembliesByPath.Clear();
            _loadedAssembliesByName.Clear();
        }

        protected void ReloadAssemblies(string[] assemblyPaths)
        {
            foreach (var assmblyPath in assemblyPaths)
            {
                var assembly = _loadContext.LoadFromAssemblyPath(assmblyPath);
                CacheLoadedAssembly(assembly);
            }
        }

        protected void Reset()
        {
            if (_loadContext != null)
            {
                _loadContext.Dispose();
            }
            var loadedAssemblies = _loadedAssembliesByPath.Keys.ToArray();
            DropCache();
            var resolver = new PathAssemblyResolver(_filePathsForLoadContext);
            _loadContext = new MetadataLoadContext(resolver);
            ReloadAssemblies(loadedAssemblies);
        }

        delegate void AssemblyLoadEventHandler(object sender, Assembly assembly);

        event AssemblyLoadEventHandler OnAssemblySuccessfullyLoaded;
    }
}
