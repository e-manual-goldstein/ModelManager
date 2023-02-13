using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public static class TypeExtensions
    {
        public static string ToUniqueTypeName(this Type type)
        {
            var assemblyName = type.Assembly.GetName();
            if (string.IsNullOrEmpty(type.FullName))
            {
                if (AssemblyLoader.IsSystemAssembly(type.Assembly.Location))
                {
                    return $"{assemblyName.Name}_{assemblyName.Version}_{type.Namespace}.{type.Name}";
                }
                else
                {
                    return $"{assemblyName.Name}_{assemblyName.Version}_{type.Namespace}.{type.Name}";
                }
            }
            return $"{assemblyName.Name}_{assemblyName.Version}_{type.Namespace}.{type.FullName}";
        }

        public static bool HasAttribute(this Type type, Type attributeType)
        {
            bool success = false;
            var attributes = type.GetCustomAttributesData();
            foreach (var attributeData in attributes)
            {
                try
                {
                    if (attributeData.AttributeType.FullName == attributeType.FullName)
                    {
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
    }
}
