using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Extensions
{
    public static class AssemblyExtensions
    {
        public static bool TryGetTargetFrameworkVersion(this Assembly assembly, out string version)
        {
            bool success = false;
            version = null;
            var attributes = assembly.GetCustomAttributesData();
            foreach (var attributeData in attributes)
            {
                try
                {
                    if (attributeData.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                    {
                        var attributeValue = attributeData.ConstructorArguments[0];
                        version = attributeValue.Value.ToString();
                        success = true;
                        break;
                    }
                }
                catch (FileNotFoundException ex)
                {

                }
                catch (Exception ex)
                {

                }
            }
            return success;
        }

        public static string ParseShortName(this string fullName)
        {
            return fullName.TryParseAssemblyName(out Match match) ? match.Groups["ShortName"].Value : null;
        }

        public static string ParseVersion(this string fullName)
        {
            return fullName.TryParseAssemblyName(out Match match) ? match.Groups["Version"].Value : null;
        }

        private static bool TryParseAssemblyName(this string assemblyFullName, out Match match)
        {
            return (match = Regex.Match(assemblyFullName, @"^(?'ShortName'.*),\s*Version=(?'Version'[\d\.]+)")).Success;            
        }
    }
}
