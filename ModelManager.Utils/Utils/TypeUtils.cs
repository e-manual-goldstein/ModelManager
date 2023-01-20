using Microsoft.EntityFrameworkCore;
using ModelManager.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ModelManager.Utils
{
	public static class TypeUtils
	{
		public static bool IsMappingClass(Type baseTypeDefinition)
		{
			var typeDefinition = baseTypeDefinition;
			while (typeDefinition.BaseType != null)
			{
				if (typeDefinition.BaseType.IsGenericType && typeDefinition.BaseType.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
				{
					return true;
				}
				typeDefinition = typeDefinition.BaseType;
			}
			return false;
		}

		public static bool InheritsFrom(this Type typeFrom, Type subType)
		{
			var type0 = typeFrom;
			while (type0.BaseType != null)
			{
				if (type0.BaseType == subType)
					return true;
				type0 = type0.BaseType;
			}
			return false;
		}

		public static bool InheritsFrom(this Type typeFrom, string subTypeName)
		{
			var type0 = typeFrom;
			while (type0.BaseType != null)
			{
				if (type0.BaseType.Name.Contains(subTypeName))
					return true;
				type0 = type0.BaseType;
			}
			return false;
		}

		public static bool HasInterface(this Type type, string interfaceName)
		{
			return type.FullName == interfaceName || type.GetInterfaces().Any(i => i.FullName == interfaceName);
		}

        public static bool ImplementsGenericInterface(this Type type, Type implemented)
		{
			var genericInterfaces = type.GetInterfaces().Where(i => i.IsGenericType);
			if (!genericInterfaces.Any())
				return false;
			return genericInterfaces.Any(i => 
				i.IsGenericType && 
				i.Name.Equals(implemented.Name) &&
				i.Assembly.Equals(implemented.Assembly) &&
				i.Namespace.Equals(implemented.Namespace));
		}

		public static void ToOutput(this List<dynamic> objectList)
		{
			//App.Manager.TabManager.DisplayOutput(objectList);
		}

		public static OutputType DetermineOutputType(object outputObject)
		{
			if (outputObject is string)
				return OutputType.Single;
			var objectList = outputObject as IEnumerable;
            var objectTable = outputObject as IDictionary;
			if (objectTable == null)
				return OutputType.List;
			return OutputType.Table;
		}

        public static Dictionary<string, IEnumerable<string>> TabulateColumns(params KeyValuePair<string,IEnumerable<string>>[] columns)
        {
            var tableDictionary = new Dictionary<string, IEnumerable<string>>();
            foreach (var column in columns)
            {
                tableDictionary.Add(column.Key, column.Value);
            }
            return tableDictionary;
        }

		public static bool IsOverrideable(this MethodInfo methodInfo)
		{
			return (methodInfo.IsVirtual || methodInfo.IsAbstract) && !methodInfo.IsFinal;
		}

		public static string RegexTypeValidationLookup(Type typeToValidate)
        {
            var dictionary = ValidTextInputTypes()
                .Zip(InputValidationRegexStrings(), (k, v) => new { k, v });
            return "";
        }

        public static List<Type> ValidTextInputTypes()
        {
            return new List<Type>()
            {
                typeof(string),
                typeof(decimal),
                typeof(int),
                typeof(double),
                typeof(float),
                typeof(byte),
                typeof(sbyte),
                typeof(char),
                typeof(long),
                typeof(short)
            };
        }

        public static List<string> InputValidationRegexStrings()
        {
            //Note RegEx patterns match the DISALLOWED inputs
            return new List<string>()
            {
                "",
                "[^0-9\\.]+",
                "[^0-9]+",
                "[^0-9\\.]+",
                "[^0-9\\.]+",
                 "[^0-9]+",
                 "[^0-9-]+",
                "[^\\w]",
                "[^0-9]+",
                "[^0-9]+",
            };
        }

        public static bool IsMandatory(this ParameterInfo parameterInfo)
        {
            return parameterInfo.GetCustomAttributes().Any(c => c is MandatoryAttribute);
        }

        
	}

	public enum OutputType
	{
		Single = 0,
		List = 1,
		Table = 2
	}

}
