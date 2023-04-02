using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AssemblyAnalyser.Tests
{
    public class DependencyTypeTests
    {
        ISpecManager _specManager;
        ILoggerProvider _loggerProvider;
        IExceptionManager _exceptionManager;
        ModuleSpec _moduleSpec;
        TypeSpec _classWithDependenciesSpec;
        
        [TestInitialize] 
        public void Initialize() 
        {
            _exceptionManager = new ExceptionManager();
            _loggerProvider = NSubstitute.Substitute.For<ILoggerProvider>();
            _specManager = new SpecManager(_loggerProvider, _exceptionManager);
            var filePath = "..\\..\\..\\..\\AssemblyAnalyser.TestData\\bin\\Debug\\net6.0\\AssemblyAnalyser.TestData.dll";
            var module = Mono.Cecil.ModuleDefinition.ReadModule(Path.GetFullPath(filePath));
            _moduleSpec = _specManager.LoadModuleSpec(module);
            _moduleSpec.Process();
            foreach (var typeSpec in _moduleSpec.TypeSpecs)
            {
                typeSpec.Process();
            }
            _specManager.ProcessLoadedProperties();
            _specManager.ProcessLoadedMethods();
            _specManager.ProcessLoadedFields();
            _specManager.ProcessLoadedParameters();
            _specManager.ProcessLoadedEvents();
            
            _classWithDependenciesSpec = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.WithDependencies.ClassWithExternalDependencies");            
        }

        #region Dependency Tests
        [TestMethod]
        public void BasicClassSpecIsNotErrorSpec_Test()
        {
            Assert.IsFalse(_classWithDependenciesSpec.IsErrorSpec);
        }

        [TestMethod]
        public void BasicClassSpecIsNotInterface_Test()
        {
            Assert.IsFalse(_classWithDependenciesSpec.IsInterface);
        }

        [TestMethod]
        public void BasicClassSpecIsNotNullSpec_Test()
        {
            Assert.IsFalse(_classWithDependenciesSpec.IsNullSpec);
        }

        [TestMethod]
        public void BasicClassSpecIsNotCompilerGenerated_Test()
        {
            Assert.IsFalse(_classWithDependenciesSpec.IsCompilerGenerated);
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneAttribute_Test()
        {
            Assert.AreEqual(1, _classWithDependenciesSpec.Attributes.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneInterface_Test()
        {
            Assert.AreEqual(1, _classWithDependenciesSpec.Interfaces.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneField_Test()
        {
            Assert.AreEqual(1, _classWithDependenciesSpec.Fields.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasNoNestedTypes_Test()
        {
            Assert.IsFalse(_classWithDependenciesSpec.NestedTypes.Any());
        }

        [TestMethod]
        public void BasicClassSpecHasThreeProperties_Test()
        {
            Assert.AreEqual(3, _classWithDependenciesSpec.Properties.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasSevenMethods_Test()
        {
            Assert.AreEqual(3, _classWithDependenciesSpec.Properties.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasTwoNonPropertyMethods_Test()
        {
            var propertyMethods = _classWithDependenciesSpec.Properties.SelectMany(c => c.InnerSpecs());
            var eventMethods = _classWithDependenciesSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _classWithDependenciesSpec.Methods.Except(propertyMethods).Except(eventMethods);

            Assert.AreEqual(2, nonPropertyMethods.Count());
        }

        [TestMethod]
        public void BasicClassSpecHasOneParameterlessMethod_Test()
        {
            var propertyMethods = _classWithDependenciesSpec.Properties.SelectMany(c => c.InnerSpecs());
            var eventMethods = _classWithDependenciesSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _classWithDependenciesSpec.Methods.Except(propertyMethods).Except(eventMethods);

            Assert.AreEqual(1, nonPropertyMethods.Where(d => !d.Parameters.Any()).Count());
        }

        [TestMethod]
        public void BasicClassSpecHasOneParameteredMethod_Test()
        {
            var propertyMethods = _classWithDependenciesSpec.Properties.SelectMany(c => c.InnerSpecs());
            var eventMethods = _classWithDependenciesSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _classWithDependenciesSpec.Methods.Except(propertyMethods).Except(eventMethods);

            Assert.AreEqual(1, nonPropertyMethods.Where(d => d.Parameters.Any()).Count());
        }

        [TestMethod]
        public void BasicClassSpecParameteredMethodHasTwoParameters_Test()
        {
            var propertyMethods = _classWithDependenciesSpec.Properties.SelectMany(c => c.InnerSpecs());
            var eventMethods = _classWithDependenciesSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _classWithDependenciesSpec.Methods.Except(propertyMethods).Except(eventMethods);
            var parameteredMethod = nonPropertyMethods.Single(d => d.Parameters.Any());

            Assert.AreEqual(2, parameteredMethod.Parameters.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneEvent_Test()
        {
            Assert.AreEqual(1, _classWithDependenciesSpec.Events.Length);
        }
        #endregion

    }
}