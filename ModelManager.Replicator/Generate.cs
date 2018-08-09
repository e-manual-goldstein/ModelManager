using StaticCodeAnalysis;
using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager.Replicator
{
    /// <summary>
    /// The Generate class contains functionality to create new code artifacts (classes, code files, code projects) based on compiled assembly code.
    /// </summary>
    public static class Generate
    {
        public static List<string> NamespaceDependencies(Type type)
        {
            var typeDependencies = new List<Type>();
            foreach (var discoveredType in dependencyList(type))
            {
                typeDependencies.AddRange(extractGenericArgumentTypes(discoveredType));
                typeDependencies.Add(discoveredType);
            }
            return typeDependencies.Select(t => t.Namespace).Distinct().ToList();
        }

        private static List<Type> dependencyList(Type type)
        {
            var dependencyList = new List<Type>();
            dependencyList.AddRange(returnTypeDependencies(type));
            dependencyList.AddRange(parameterTypeDependencies(type));
            dependencyList.AddRange(extendedInterfaceDependencies(type));
            if (type.BaseType != typeof(object) && type.BaseType != null)
                dependencyList.Add(type.BaseType);
            return dependencyList.Distinct().ToList();
        }

        private static List<Type> returnTypeDependencies(Type type)
        {
            var methodReturnTypes = type.GetMethods().Select(m => m.ReturnType);
            var propertyReturnTypes = type.GetProperties().Select(p => p.PropertyType);
            return methodReturnTypes.Union(propertyReturnTypes).Distinct().ToList();
        }

        private static List<Type> parameterTypeDependencies(Type type)
        {
            var methodParameterTypes = type.GetMethods().SelectMany(m => m.GetParameters());
            return methodParameterTypes.Select(p => p.ParameterType).Distinct().ToList();
        }

        private static List<Type> extendedInterfaceDependencies(Type type)
        {
            return type.GetInterfaces().SelectMany(m => dependencyList(m)).ToList();
        }

        private static List<Type> extractGenericArgumentTypes(Type type)
        {
            var typeList = new List<Type>() { type };
            if (!type.IsGenericType)
                return typeList;
            foreach (var argumentType in type.GetGenericArguments())
            {
                typeList.AddRange(extractGenericArgumentTypes(argumentType));
                typeList.Add(argumentType);
            }
            return typeList;
        }

        #region Code File Generator

        [Obsolete]
        public static string CodeFileFromInterface(string fileName, Type interfaceType, InterfaceDefinition interfaceDefinition)
        {
            var codeFileContents = new StringBuilder();
            IStaticCodeElement element = interfaceDefinition;
            while (!(element.Owner is CodeFile))
            {
                element = element.Owner;
            }
            var codeFile = element.Owner as CodeFile;
            foreach (var declaration in codeFile.UsingDeclarations)
            {
                codeFileContents.AppendLine("using " + declaration + ";");
            }
            codeFileContents.AppendLine("namespace " + interfaceDefinition.Namespace);
            codeFileContents.AppendLine("{");
            codeFileContents.Append(ClassFromInterface(interfaceType));
            codeFileContents.AppendLine("}");
            return codeFileContents.ToString();
        }
        
        /// <summary>
        /// Generate the contents of a mock code file and class which meet the minimum requirements to implement the provided interface
        /// </summary>
        /// <param name="interfaceType">Interface to mock</param>
        /// <returns></returns>
        public static string CodeFileFromInterface(Type interfaceType)
        {
            var codeFileContents = new StringBuilder();
            var namespaces = NamespaceDependencies(interfaceType);
            namespaces.Add(interfaceType.Namespace); //Do this because the newly created class will not be in the interface's namespace
            foreach (var declaration in namespaces.Distinct())
            {
                codeFileContents.AppendLine("using " + declaration + ";");
            }
            codeFileContents.AppendLine();
            codeFileContents.AppendLine("namespace " + interfaceType.Namespace + ".MockImpl");
            codeFileContents.AppendLine("{");
            codeFileContents.Append(ClassFromInterface(interfaceType));
            codeFileContents.AppendLine("}");
            return codeFileContents.ToString();
        }
        
        public static string ClassFromInterface(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Argument must be an interface type to replicate");
            var name = createNameFromInterface(interfaceType.Name);
            var modifiers = interfaceType.GetModifiers();
            var members = interfaceType.GetMembers().ToList();
            members.AddRange(interfaceType.GetInterfaces().SelectMany(i => i.GetMembers()));
            return Replicate.Class(name, modifiers, members.ToArray(), interfaceType);
        }

        private static string createNameFromInterface(string name)
        {
            if (name.StartsWith("I"))
                return name.Substring(1);
            return "Fake" + name + "Impl";
        }

        /// <summary>
        /// Generate the contents of a mock code file and class which meet the minimum requirements to implement the provided abstract class
        /// </summary>
        /// <param name="interfaceType">Interface to mock</param>
        /// <returns></returns>
        public static string CodeFileFromAbstractClass(Type classType)
        {

            var codeFileContents = new StringBuilder();
            //var namespaces = NamespaceDependencies(classType);
            //namespaces.Add(classType.Namespace); //Do this because the newly created class will not be in the interface's namespace
            //foreach (var declaration in namespaces.Distinct())
            //{
            //    codeFileContents.AppendLine("using " + declaration + ";");
            //}
            //codeFileContents.AppendLine("namespace " + classType.Namespace + ".MockImpl");
            //codeFileContents.AppendLine("{");
            //codeFileContents.Append(ClassFromInterface(classType));
            //codeFileContents.AppendLine("}");
            return codeFileContents.ToString();
        }

        #endregion

        #region Project Generator

        /// <summary>
        /// Used to generate a set of mock code files from an assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="interfacesOnly"></param>
        public static Dictionary<string, string> CodeFileStringsFromAssembly(Assembly assembly, bool interfacesOnly = true)
        {
            var codeFiles = new Dictionary<string, string>();
            var typesToMock = assembly.GetTypes().Where(t => interfacesOnly ? t.IsInterface : true);
            foreach (var type in typesToMock)
            {
                codeFiles.Add(createNameFromInterface(type.Name), CodeFileFromInterface(type));
            }
            return codeFiles;
        }

        private static List<string> createCodeFiles(string rootFolder, Dictionary<string, string> codeFileDictionary)
        {
            var codeFilePaths = new List<string>();
            if (!Directory.Exists(rootFolder))
                Directory.CreateDirectory(rootFolder);
            foreach (var codeFileEntry in codeFileDictionary)
            {
                var codeFilePath = Path.Combine(rootFolder, createNameFromInterface(codeFileEntry.Key) + ".cs");
                File.WriteAllText(codeFilePath, codeFileEntry.Value);
            }
            return codeFilePaths;
        }
        
        public static string ProjectFromAssembly(string filePath, string outputDirectory = null)
        {
            var assembly = Assembly.LoadFile(filePath);
            var codeFiles = CodeFileStringsFromAssembly(assembly);
            var root = Directory.GetDirectoryRoot(AppDomain.CurrentDomain.BaseDirectory);
            var directory = outputDirectory ?? Path.Combine(root, "Temp", assembly.GetName().Name);
            var codeFilePaths = createCodeFiles(root, codeFiles);
            var baseProjectFile = File.ReadAllText("BaseProject.txt");
            return projectFileFromAssembly(assembly);
        }

        private static string projectFileFromAssembly(Assembly assembly)
        {
            var baseProjectFile = File.ReadAllText("BaseProject.txt");
            //inject code files
            //inject reference dependencies
            return baseProjectFile;
        }

        #endregion
    }
}
