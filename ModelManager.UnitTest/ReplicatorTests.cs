using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelManager.Replicator;
using StaticCodeAnalysis;
using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager.UnitTest
{
    [TestClass]
    public class ReplicatorTests
    {
        [TestMethod]
        public void Method_Test()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("ModelManager"));
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods().Where(m => m.DeclaringType == type && !m.IsSpecialName))
                    {
                        var methodBlock = Replicate.Method(method);
                    }
                }
            }
        }

        [TestMethod]
        public void Property_Test()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("ModelManager"));
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var property in type.GetProperties().Where(m => m.DeclaringType == type))
                    {
                        var propertyBlock = Replicate.Property(property);
                    }
                }
            }
        }

        [TestMethod]
        public void Class_Test()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("StaticCode"));
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes().Where(t => t.IsClass))
                {
                    var fakeClass = Replicate.Class(type);
                }
            }
        }

        [TestMethod]
        public void ClassFromInterface_Test()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("StaticCode"));
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes().Where(t => t.IsInterface))
                {
                    var fakeClass = Generate.ClassFromInterface(type);
                }
            }
        }

        [TestMethod, Obsolete]
        public void CodeFileFromInterfaceWithStaticCodeAnalysis_Test()
        {
            var filePath = @"..\..\..\StaticCodeAnalysis\Interfaces\IStaticCodeElement.cs";
            var interfaceName = "IStaticCodeElement";
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.Contains("StaticCode"));
            var testInterface = assembly.GetTypes().FirstOrDefault(t => t.Name == interfaceName);
            var codeFile = CodeUtils.ReadCodeFile(filePath);
            var interfaceDefinition = codeFile.DefinedTypes.OfType<InterfaceDefinition>().FirstOrDefault(i => i.Name == interfaceName);
            var replica = Generate.CodeFileFromInterface(filePath, testInterface, interfaceDefinition);
        }

        [TestMethod]
        public void CodeFileFromInterface_Test()
        {
            //var filePath = @"..\..\..\StaticCodeAnalysis\Interfaces\IStaticCodeElement.cs";
            var interfaceName = "IStaticCodeElement";
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.Contains("StaticCode"));
            var testInterface = assembly.GetTypes().FirstOrDefault(t => t.Name == interfaceName);
            //var codeFile = CodeUtils.ReadCodeFile(filePath);
            //var interfaceDefinition = codeFile.DefinedTypes.OfType<InterfaceDefinition>().FirstOrDefault(i => i.Name == interfaceName);
            var replica = Generate.CodeFileFromInterface(testInterface);
        }

        [TestMethod]
        public void CodeFilesFromAssembly_Test()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.Contains("ModelManager"));
            Generate.CodeFileStringsFromAssembly(assembly);

            
        }


    }
}
