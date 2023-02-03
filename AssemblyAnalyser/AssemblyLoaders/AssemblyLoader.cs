using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public abstract class AssemblyLoader
    {
        public static AssemblyLoader GetLoader(string targetFrameworkVersion, string imageRuntimeVersion)
        {
            if (_imageRuntimeLoaders.TryGetValue(imageRuntimeVersion, out var loaders))
            {
                if (loaders.Count > 1)
                {
                    //find best loader
                }
                else
                {
                    return loaders.Single();
                }
            }
            else if (_frameworkVersionLoaders.TryGetValue(targetFrameworkVersion, out loaders))
            {
                if (loaders.Count > 1)
                {
                    //find best loader
                }
                else
                {
                    return loaders.Single();
                }
            }

            var extraGAC = new List<string>()
            {
                "C:\\Windows\\assembly\\GAC_MSIL\\Microsoft.IdentityModel\\3.5.0.0__31bf3856ad364e35\\Microsoft.IdentityModel.dll",

            };
            return new DotNetFrameworkLoader("v4.0.30319", extraGAC);
        }

        public abstract Assembly LoadAssemblyByName(string assemblyName);

        public abstract Assembly LoadAssemblyByPath(string assemblyPath);

        public abstract IEnumerable<Assembly> LoadReferencedAssembliesByRootPath(string rootAssemblyPath);

        private static IDictionary<string, List<AssemblyLoader>> _imageRuntimeLoaders = new Dictionary<string, List<AssemblyLoader>>();

        private static IDictionary<string, List<AssemblyLoader>> _frameworkVersionLoaders = new Dictionary<string, List<AssemblyLoader>>();


    }
}
