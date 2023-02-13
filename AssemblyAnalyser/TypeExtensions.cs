using System;
using System.Collections.Generic;
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
            return $"{assemblyName.Name}_{assemblyName.Version}_{type.FullName}";
        }
    }
}
