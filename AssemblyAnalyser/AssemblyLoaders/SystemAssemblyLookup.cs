using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AssemblyAnalyser.AssemblyLoaders
{
    public static class SystemAssemblyLookup
    {
        public static bool TryGetPath(string assemblyFullName, out string filePath)
        {
            filePath = FindAssemblyInGAC(assemblyFullName);
            return !string.IsNullOrEmpty(filePath) ||
                _systemAssemblyPaths.TryGetValue(assemblyFullName, out filePath);
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

        private static string FindAssemblyInGAC(string assemblyFullName)
        {
            var gacFiles = Directory.GetFiles("C:\\Windows\\assembly\\", "*.dll", SearchOption.AllDirectories);
            var assemblyMatch = ParseAssemblyName(assemblyFullName);
            var matchingFiles = gacFiles.Where(d => Path.GetFileNameWithoutExtension(d)
                .Equals(assemblyMatch.Groups["ShortName"].Value,StringComparison.CurrentCultureIgnoreCase));
            if (matchingFiles.Any())
            {
                foreach (var match in matchingFiles)
                {
                    if (match.Contains(assemblyMatch.Groups["Version"].Value))
                    {
                        return match;
                    }
                }
            }
            return null;
        }

        private static Match ParseAssemblyName(string assemblyFullName)
        {
            return Regex.Match(assemblyFullName, @"^(?'ShortName'.*),\s*Version=(?'Version'[\d\.]+)");
        }  
    }
}
