using Microsoft.EntityFrameworkCore;
using ModelManager.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ModelManager.Utils
{
	public static class AssemblyUtils
	{
		static AssemblyName[] _myAssemblies;

		static string _version;

		static AssemblyUtils()
		{
			_myAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies().ToArray();
			//_version = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
            
        }

        public static void RedirectAssembly(string shortName, Version targetVersion, string publicKeyToken)
        {
            ResolveEventHandler handler = null;

            handler = (sender, args) => {
				try
				{

					// Use latest strong name & version when trying to load SDK assemblies
					var requestedAssembly = new AssemblyName(args.Name);
					if (requestedAssembly.Name != shortName)
						return null;

					Debug.WriteLine("Redirecting assembly load of " + args.Name
								  + ",\tloaded by " + (args.RequestingAssembly == null ? "(unknown)" : args.RequestingAssembly.FullName));

					requestedAssembly.Version = targetVersion;
					requestedAssembly.SetPublicKeyToken(new AssemblyName("x, PublicKeyToken=" + publicKeyToken).GetPublicKeyToken());
					requestedAssembly.CultureInfo = CultureInfo.InvariantCulture;
					AppDomain.CurrentDomain.AssemblyResolve -= handler;
					return Assembly.Load(requestedAssembly);
				}
				finally
				{
                    AppDomain.CurrentDomain.AssemblyResolve -= handler;
                }
            };
            AppDomain.CurrentDomain.AssemblyResolve += handler;
        }

		public static void AutoRedirect(string fileName, Assembly assembly)
		{
            var info = fileName.Split(',');
			var assemblyName = info[0].Trim();
			var newAssembly = assembly.GetReferencedAssemblies().Last(a => a.Name == assemblyName);
			var publicKeyToken = info[3].Trim().Split('=')[1];
            var version = newAssembly.Version;
            RedirectAssembly(info[0].Trim(), newAssembly.Version, publicKeyToken);
        }

        public static Assembly LoadAssembly(string assemblyPath)
		{
			return Assembly.LoadFrom(assemblyPath);
		}

		public static List<Type> LoadTypes(this Assembly assembly)
		{
			Type[] types;
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				types = e.Types;
			}
			return types.Where(t => t != null).ToList();
		}

		public static Assembly LoadFullAssembly(this string assemblyPath, List<string> checkedNames, Dictionary<string, Assembly> cache = null)
		{
			var directory = Path.GetDirectoryName(assemblyPath);
			Assembly assembly = null;
			try
			{
				if (File.Exists(assemblyPath))
				{
					assembly = Assembly.LoadFrom(assemblyPath);
				}
				else
				{
					assembly = Assembly.Load(assemblyPath);
				}
				if (cache == null)
				{
					cache = new Dictionary<string, Assembly>();
				}
				foreach (var reference in assembly.GetReferencedAssemblies())
				{
					if (_myAssemblies.Any(a => a.FullName == reference.FullName))
					{
						continue;
					}
					var target = LocateAssembly(directory, reference);
					if (!checkedNames.Contains(target))
					{
						checkedNames.Add(target);
						var referencedAssembly = LoadFullAssembly(target, checkedNames, cache);
						cache[target] = referencedAssembly;
					}
				}
			}
			catch
			{

			}
			return assembly;
		}

		private static string LocateAssembly(string directory, AssemblyName reference)
		{
			var assemblyFileName = $"{reference.Name}.dll";

            var target = Path.Combine(directory, assemblyFileName);
			if (File.Exists(target))
			{
				return target;
			}
			var files =  Directory.EnumerateFiles("C:\\Windows\\Microsoft.NET\\", "*.dll", SearchOption.AllDirectories).Where(t => Path.GetFileName(t) == assemblyFileName);
			return reference.FullName;
        }

		//public static List<Assembly> RGetReferencedAssemblies(this Assembly assembly)
		//{
		//	return assembly.GetReferencedAssemblies().SelectMany(a => a)
		//}

		public static IEnumerable<TypeInfo> GetPublicTypesFromAssembly(Assembly assembly, bool includeNested)
		{
			return assembly.DefinedTypes.Where(x => x.IsPublic || (x.IsNested && includeNested));
		}

		/// <summary>
		/// Uses DbContext and DbModelBuilder for a particular assembly to return a Type Dictionary of mapped entities.
		/// "Keys" contains mapped types.
		/// "Values" contains a list of Ignored Properties for each type.
		/// </summary>
		/// <param name="assembly"></param>
		/// <returns></returns>
		public static Dictionary<Type, IEnumerable<PropertyInfo>> MappedTypesWithIgnoredProperties(this Assembly assembly)
		{
			var types = assembly.GetTypes();
			var mappedTypes = new List<Type>();
			var typeDictionary = new Dictionary<Type, IEnumerable<PropertyInfo>>();
			foreach (var type in types)
			{
				var type0 = type;
				while (type0.BaseType != null)
				{
					//if (type0.BaseType.Name == "DbContext")
					//{
					//	var dbContext = Activator.CreateInstance(type) as DbContext;
					//	var modelBuilder = new DbModelBuilder();
					//	var modelCreateAction = dbContext.GetType().GetRuntimeMethods().First(c => c.Name == "OnModelCreating");
					//	modelCreateAction.Invoke(dbContext, new[] { modelBuilder });
					//	var modelConfiguration = modelBuilder.Configurations.GetType().GetRuntimeFields().First()
					//		.GetValue(modelBuilder.Configurations);
					//	var entityTypes = modelConfiguration.GetType().GetRuntimeProperties().First(c => c.Name == "ActiveEntityConfigurations")
					//		.GetValue(modelConfiguration) as ICollection;
					//	foreach (var entityType in entityTypes)
					//	{
					//		var clrType = entityType.GetType().GetRuntimeProperties().First(c => c.Name == "ClrType").GetValue(entityType) as Type;
					//		var ignoredProperties = entityType.GetType().GetRuntimeProperties().First(c => c.Name == "IgnoredProperties").GetValue(entityType) as IEnumerable<PropertyInfo>;
					//		typeDictionary.Add(clrType, ignoredProperties);
					//	}
						
					//	break;
					//}
					type0 = type0.BaseType;
				}
			}
			return typeDictionary;
		}

		public static List<MappedEntity> MappedTypesWithDatabaseInfo(this Assembly assembly, string namespaceFilter, string objectNameFilter, string database, string inheritsFromType, string implementsInterface)
		{
			var types = assembly.GetTypes();
			
			var mappedTypes = new List<MappedEntity>();
			foreach (var type in types)
			{
				var type0 = type;
				while (type0.BaseType != null)
				{
					if (type0.BaseType.Name == "DbContext" && !type.IsAbstract && type.GetConstructors().Any(cc => !cc.GetParameters().Any()))
					{
						//try
						//{
						//	var dbContext = Activator.CreateInstance(type) as DbContext;


						//	var contextName = type.FullName;
						//	var connectionString = dbContext.GetType().GetRuntimeProperty("ConnectionString").GetValue(dbContext);

						//	var modelBuilder = new DbModelBuilder();
						//	var modelCreateAction = dbContext.GetType().GetRuntimeMethods().First(c => c.Name == "OnModelCreating");
						//	modelCreateAction.Invoke(dbContext, new[] { modelBuilder });
						//	var modelConfiguration = modelBuilder.Configurations.GetType().GetRuntimeFields().First()
						//		.GetValue(modelBuilder.Configurations);
						//	var mappingInstances = modelConfiguration.GetType().GetRuntimeProperties().First(c => c.Name == "ActiveEntityConfigurations")
						//		.GetValue(modelConfiguration) as ICollection;

						//	foreach (var mappingInstance in mappingInstances)
						//	{
						//		var mappedType = new MappedEntity(mappingInstance);
						//		GetMappingInfoForType(mappedType, connectionString.ToString());
						//		if (applyFilter(mappedType, namespaceFilter, objectNameFilter, database, inheritsFromType, implementsInterface))
						//			mappedTypes.Add(mappedType);
						//	}
						//}
						//catch
						//{

						//}
						break;
					}
					type0 = type0.BaseType;
				}
			}

			return mappedTypes;
		}

		private static bool applyFilter(MappedEntity mappedType, string namespaceFilter, string objectNameFilter, string database, string inheritsFromType, string implementsInterface)
		{
			if (namespaceFilter != null && !mappedType.Namespace.Contains(namespaceFilter))
				return false;
			if (objectNameFilter != null && !mappedType.ObjectName.Contains(objectNameFilter))
				return false;
			if (database != null && !mappedType.DatabaseName.Contains(database))
				return false;
			if (!string.IsNullOrEmpty(implementsInterface) && !mappedType.EntityType.HasInterface(implementsInterface))
				return false;
			if (!string.IsNullOrEmpty(inheritsFromType) && !mappedType.EntityType.InheritsFrom(inheritsFromType))
				return false;
			return true;
		}

		public static void GetMappingInfoForType(MappedEntity entityType, string databaseName)
		{
			GetTableAndSchemaNameByReflection(entityType);
			entityType.DatabaseName = databaseName;
		}

		public static void GetTableAndSchemaNameByReflection(MappedEntity entity)
		{
			var configMember = entity.MapInstance.GetType().GetProperties().FirstOrDefault(c => c.Name == "Configuration");
			var tableNameProp = entity.MapInstance.GetType().GetProperty("TableName").GetValue(entity.MapInstance, null);
			if (tableNameProp != null)
				entity.TableName = tableNameProp.ToString();
			else if (configMember != null)
			{
				var config = configMember.GetValue(entity.MapInstance, null);
				var tableProp = config.GetType().GetProperty("TableName").GetValue(config, null);
				if (tableProp != null)
					entity.TableName = tableProp.ToString();
			}
			var schemaProp = entity.MapInstance.GetType().GetProperty("SchemaName").GetValue(entity.MapInstance, null);
			if (schemaProp == null && configMember != null)
			{
				var config = configMember.GetValue(entity.MapInstance, null);
				schemaProp = config.GetType().GetProperty("SchemaName").GetValue(config, null);
			}
			entity.Schema = schemaProp != null ? schemaProp.ToString() : "dbo";
		}
		
		public static List<PropertyInfo> RetrieveAllProperties(List<TypeInfo> types)
		{
			//TODO: This doesn't seem to have much of a purpose
			var allProperties = new List<PropertyInfo>();
			foreach (var type in types)
			{
				var properties = type.GetProperties();
				allProperties.AddRange(properties);
			}
			return allProperties;
		}

		//TODO: Is this useful?
		public static List<PropertyInfo> RetrieveAllProperties(Type[] types)
		{
			var allProperties = new List<PropertyInfo>();
			foreach (var type in types)
			{
				var properties = type.GetProperties();
				allProperties.AddRange(properties);
			}
			return allProperties;
		}

		//TODO: Is this useful?
		public static List<MethodInfo> RetrieveAllMethods(List<TypeInfo> types)
		{
			var allMethods = new List<MethodInfo>();
			allMethods = types.SelectMany(c => c.GetMethods()).ToList();
			return allMethods;
		}


		//TODO: Reimplement This
		public static void TableView(string output = "Console", params IEnumerable<string>[] inputs)
		{
			int j = 0;
			int[] widths = new int[inputs.Count() + 1];
			var outputString = new StringBuilder();
			foreach (var list in inputs)
			{
				j++;
				var width = list.Select(l => l != null ? l.Length : 0).Max();
				widths[j] = width;
			}
			var length = inputs.Max(l => l.Count());
			for (int i = 0; i < length;)
			{
				string lineEntry = "";
				int k = 0;
				foreach (var input in inputs)
				{
					var toList = input.ToList();
					k++;
					var cell = toList[i] ?? "";
					lineEntry += cell.PadRight(widths[k]) + "    ";
				}
				i++;
				if (output == "Text")
					outputString.Append(lineEntry + "\n");
				Console.WriteLine(lineEntry);
			}
		}


	}
}
