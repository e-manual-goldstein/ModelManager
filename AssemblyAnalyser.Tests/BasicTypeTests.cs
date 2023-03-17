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
    public class BasicTypeTests
    {
        ISpecManager _specManager;
        ILoggerProvider _loggerProvider;
        IExceptionManager _exceptionManager;
        ModuleSpec _moduleSpec;
        TypeSpec _basicClassSpec;
        TypeSpec _basicInterfaceSpec;
        TypeSpec _basicAttribute;
        TypeSpec _basicDelegate;

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
            //_specManager.ProcessLoadedAttributes();
            _basicClassSpec = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.BasicClass");
            _basicInterfaceSpec = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.IBasicInterface");
            _basicAttribute = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.BasicAttribute");
            _basicDelegate = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.BasicDelegate");
        }

        #region Basic Class Tests
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
            var eventMethods = _basicClassSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _basicClassSpec.Methods.Except(propertyMethods).Except(eventMethods);

            Assert.AreEqual(2, nonPropertyMethods.Count());
        }

        [TestMethod]
        public void BasicClassSpecHasOneParameterlessMethod_Test()
        {
            var propertyMethods = _basicClassSpec.Properties.SelectMany(c => c.InnerSpecs());
            var eventMethods = _basicClassSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _basicClassSpec.Methods.Except(propertyMethods).Except(eventMethods);

            Assert.AreEqual(1, nonPropertyMethods.Where(d => !d.Parameters.Any()).Count());
        }

        [TestMethod]
        public void BasicClassSpecHasOneParameteredMethod_Test()
        {
            var propertyMethods = _basicClassSpec.Properties.SelectMany(c => c.InnerSpecs());
            var eventMethods = _basicClassSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _basicClassSpec.Methods.Except(propertyMethods).Except(eventMethods);

            Assert.AreEqual(1, nonPropertyMethods.Where(d => d.Parameters.Any()).Count());
        }

        [TestMethod]
        public void BasicClassSpecParameteredMethodHasTwoParameters_Test()
        {
            var propertyMethods = _basicClassSpec.Properties.SelectMany(c => c.InnerSpecs());
            var eventMethods = _basicClassSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _basicClassSpec.Methods.Except(propertyMethods).Except(eventMethods);
            var parameteredMethod = nonPropertyMethods.Single(d => d.Parameters.Any());

            Assert.AreEqual(2, parameteredMethod.Parameters.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneEvent_Test()
        {
            Assert.AreEqual(1, _basicClassSpec.Events.Length);
        }
        #endregion

        #region Basic Interface Tests

        [TestMethod]
        public void IBasicInterfaceIsInterface_Test()
        {
            Assert.IsTrue(_basicInterfaceSpec.IsInterface);
        }

        [TestMethod]
        public void IBasicInterfaceHasExactlyOneImplementation_Test()
        {
            Assert.AreEqual(1, _basicInterfaceSpec.Implementations.Length);
        }

        #endregion

        #region Basic Attribute Tests
        
        [TestMethod]
        public void BasicAttributeDecoratesExactlyOneType_Test()
        {
            Assert.AreEqual(1, _basicAttribute.DecoratorForSpecs.OfType<TypeSpec>().Count());
        }

        #endregion

        #region Basic Delegate Tests

        [TestMethod]
        public void BasicDelegateIsDelegateForExactlyOneType_Test()
        {
            Assert.AreEqual(1, _basicDelegate.DelegateForSpecs.Count());
        }

        #endregion
    }
}