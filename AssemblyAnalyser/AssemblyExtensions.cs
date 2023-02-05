using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
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
                catch (Exception ex)
                {
                }
            }
            return success;
        }
    }
}
