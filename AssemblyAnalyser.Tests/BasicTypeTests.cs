using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AssemblyAnalyser.Tests
{
    [TestClass]
    public class BasicClassTests
    {
        ISpecManager _specManager;
        ILoggerProvider _loggerProvider;
        IExceptionManager _exceptionManager;
        AssemblySpec _assemblySpec;
        TypeSpec _basicClassSpec;

        [TestInitialize] 
        public void Initialize() 
        {
            _exceptionManager = new ExceptionManager();
            _loggerProvider = NSubstitute.Substitute.For<ILoggerProvider>();
            _specManager = new SpecManager(_loggerProvider, _exceptionManager);
            var filePath = "..\\..\\..\\..\\AssemblyAnalyser.TestData\\bin\\Debug\\net6.0\\AssemblyAnalyser.TestData.dll";
            var assembly = Assembly.LoadFile(Path.GetFullPath(filePath));
            _assemblySpec = _specManager.LoadAssemblySpec(assembly);
            _assemblySpec.Process();
            foreach (var typeSpec in _assemblySpec.TypeSpecs)
            {
                typeSpec.Process();
            }
            _specManager.ProcessLoadedProperties();
            _specManager.ProcessLoadedMethods();
            _specManager.ProcessLoadedFields();
            _specManager.ProcessLoadedParameters();
            _basicClassSpec = _assemblySpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.BasicClass");
        }
        
        [TestMethod]
        public void BasicClassSpecIsNotErrorSpec_Test()
        {
            Assert.IsFalse(_basicClassSpec.IsErrorSpec);            
        }

        [TestMethod]
        public void BasicClassSpecIsNotInterface_Test()
        {
            Assert.IsFalse(_basicClassSpec.IsInterface);            
        }

        [TestMethod]
        public void BasicClassSpecIsNotNullSpec_Test()
        {
            Assert.IsFalse(_basicClassSpec.IsNullSpec);            
        }

        [TestMethod]
        public void BasicClassSpecIsNotCompilerGenerated_Test()
        {
            Assert.IsFalse(_basicClassSpec.IsCompilerGenerated);
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneAttribute_Test()
        {
            Assert.AreEqual(1, _basicClassSpec.Attributes.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneInterface_Test()
        {
            Assert.AreEqual(1, _basicClassSpec.Interfaces.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneField_Test()
        {
            Assert.AreEqual(1, _basicClassSpec.Fields.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasNoNestedTypes_Test()
        {
            Assert.IsFalse(_basicClassSpec.NestedTypes.Any());
        }

        [TestMethod]
        public void BasicClassSpecHasThreeProperties_Test()
        {
            Assert.AreEqual(3, _basicClassSpec.Properties.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasSevenMethods_Test()
        {
            Assert.AreEqual(3, _basicClassSpec.Properties.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasTwoNonPropertyMethods_Test() 
        {
            var propertyMethods = _basicClassSpec.Properties.SelectMany(c => c.InnerSpecs());
            Assert.AreEqual(2, _basicClassSpec.Methods.Except(propertyMethods).Count());
        }

        [TestMethod]
        public void BasicClassSpecHasOneParameterlessMethod_Test()
        {
            var propertyMethods = _basicClassSpec.Properties.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _basicClassSpec.Methods.Except(propertyMethods);
            
            Assert.AreEqual(1, nonPropertyMethods.Where(d => !d.Parameters.Any()).Count());
        }

        [TestMethod]
        public void BasicClassSpecHasOneParameteredMethod_Test()
        {
            var propertyMethods = _basicClassSpec.Properties.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _basicClassSpec.Methods.Except(propertyMethods);

            Assert.AreEqual(1, nonPropertyMethods.Where(d => d.Parameters.Any()).Count());
        }

        [TestMethod]
        public void BasicClassSpecParameteredMethodHasTwoParameters_Test()
        {
            var propertyMethods = _basicClassSpec.Properties.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _basicClassSpec.Methods.Except(propertyMethods);
            var parameteredMethod = nonPropertyMethods.Single(d => d.Parameters.Any());
            
            Assert.AreEqual(2, parameteredMethod.Parameters.Length);
        }
    }
}