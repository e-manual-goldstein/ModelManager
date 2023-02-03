using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.AssemblyLoaders
{
    public static class SystemAssemblyLookup
    {
        public static bool TryGetPath(string assemblyFullName, out string filePath)
        {
            return _systemAssemblyPaths.TryGetValue(assemblyFullName, out filePath);
        }

        private static Dictionary<string, string> _systemAssemblyPaths = CreateSystemPathLookup();

        private static Dictionary<string, string> CreateSystemPathLookup()
        {
            return new Dictionary<string, string>()
            {
                { "Microsoft.IdentityModel, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                    "C:\\Windows\\assembly\\GAC_MSIL\\Microsoft.IdentityModel\\3.5.0.0__31bf3856ad364e35\\Microsoft.IdentityModel.dll" },
                //{ "Microsoft.Web.Infrastructure, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                
                //    "" },
            };
        }
    }
}
