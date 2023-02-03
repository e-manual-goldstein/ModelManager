using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.AssemblyLoaders
{
    public class AssemblyLoader
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
            throw new NotSupportedException();
        }

        private static IDictionary<string, List<AssemblyLoader>> _imageRuntimeLoaders = new Dictionary<string, List<AssemblyLoader>>();

        private static IDictionary<string, List<AssemblyLoader>> _frameworkVersionLoaders = new Dictionary<string, List<AssemblyLoader>>();


    }
}
