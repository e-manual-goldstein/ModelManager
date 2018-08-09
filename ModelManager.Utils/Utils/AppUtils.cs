using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace ModelManager.Utils
{
	public static class AppUtils
	{


        public static bool IsFirstRun()
        {
            var setting = ConfigurationManager.AppSettings["FirstRun"];
            bool isFirstRun;
            //noSetting is TRUE when "FirstRun" key is anything but TRUE/FALSE
            var noSetting = !bool.TryParse(setting, out isFirstRun);
            return noSetting || isFirstRun;
        }

        public static T GetAppSetting<T>(string settingName)
        {
            var setting = ConfigurationManager.AppSettings[settingName];
            try
            {
                return (T)Convert.ChangeType(setting, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        public static string CreateDisplayString(string inputString, int? truncateTo = null)
        {
            var firstPass = Regex.Replace(inputString, "([A-Z])([a-z])", m => " " + m.Groups[0].Value).Trim();
            var secondPass = Regex.Replace(firstPass, "([a-z])([A-Z])", m => m.Groups[1].Value + " " + m.Groups[2].Value).Trim();
            var pascalCase = Regex.Replace(secondPass, "^([a-z])", m => m.Groups[0].Value.ToUpper());
            return truncateTo.HasValue && pascalCase.Length >= truncateTo.Value 
                ? pascalCase.Substring(0, truncateTo.Value) + "..." 
                : pascalCase;
        }

		public static Dictionary<string, IEnumerable<string>> DisplayAllProperties(object target)
		{
			var propertyList = target.GetType().GetRuntimeProperties().ToList();
			var propertyNames = propertyList.Select(c => c.Name).ToList();
			var propertyValues = propertyList.Select(c => c.GetValue(target)?.ToString());
			var table = new Dictionary<string, IEnumerable<string>>();
			table.Add("Names", propertyNames);
			table.Add("Values", propertyValues);
			return table; //App.Manager.TabManager.DisplayOutput(table, null);
		}
	
        public static Dictionary<string, IEnumerable<string>> CompareTo(this Type baseType, Type comparedType)
        {
            var names = new List<string>();
            var baseTypeProperties = new List<string>();
            var comparedTypeProperties = new List<string>();
            var props = typeof(Type).GetProperties();
            foreach (var property in baseType.GetType().GetProperties().ToList())
            {
                try
                {
                    var name = property.Name;
                    var baseTypeProperty = property.GetValue(baseType).ToString();
                    var comparedTypeProperty = property.GetValue(comparedType).ToString();
                    names.Add(name);
                    baseTypeProperties.Add(baseTypeProperty);
                    comparedTypeProperties.Add(comparedTypeProperty);
                }
                catch
                {

                }
            }
            var table = new Dictionary<string, IEnumerable<string>>();
            table.Add("Names", names);
            table.Add("BaseTypeProperties", baseTypeProperties);
            table.Add("ComparedTypeProperties", comparedTypeProperties);
            return table;
        }
    }
}
