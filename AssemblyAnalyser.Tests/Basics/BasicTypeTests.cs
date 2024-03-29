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
    public class BasicTypeTests : AbstractSpecTests
    {
        TypeSpec _basicAttribute;
        TypeSpec _basicDelegate;

        [TestInitialize]
        public override void Initialize() 
        {
            base.Initialize();
            var types = _moduleSpec.TypeSpecs;
            _specManager.ProcessSpecs(types, false);
            var methods = types.SelectMany(t => t.Methods);
            var properties = types.SelectMany(t => t.Properties);
            _specManager.ProcessSpecs(methods);
            _specManager.ProcessSpecs(properties);
            var fields = types.SelectMany(t => t.Fields);
            _specManager.ProcessSpecs(fields);
            var parameters = methods.SelectMany(m => m.Parameters).Union(properties.SelectMany(p => p.Parameters));
            _specManager.ProcessSpecs(parameters, false);
            var events = types.SelectMany(m => m.Events);
            _specManager.ProcessSpecs(events);
            //_specManager.ProcessLoadedAttributes();
            _basicAttribute = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.BasicAttribute");
            _basicDelegate = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.BasicDelegate");
        }

        #region Basic Class Tests
        [TestMethod]
        public void BasicClassSpecIsNotErrorSpec_Test()
        {
            Assert.IsFalse(_basicClassSpec.IsMissingSpec);
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
        public void BasicClassSpecHasBasicAttribute_Test()
        {
            Assert.AreEqual(1, _basicClassSpec.Attributes.Where(a => a.Name == "BasicAttribute").Count());
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneInterface_Test()
        {
            Assert.AreEqual(1, _basicClassSpec.Interfaces.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneField_Test()
        {
            Assert.AreEqual(1, _basicClassSpec.Fields.Where(f => !f.IsBackingField && !f.IsEventField).Count());
        }

        [TestMethod]
        public void BasicClassSpecHasNoNestedTypes_Test()
        {
            Assert.IsFalse(_basicClassSpec.NestedTypes.Any());
        }

        [TestMethod]
        public void BasicClassSpecHasManyProperties_Test()
        {
            Assert.IsTrue(_basicClassSpec.Properties.Any());
        }

        [TestMethod]
        public void BasicClassSpecHasManyBackingFields_Test()
        {
            Assert.IsTrue(_basicClassSpec.Fields.Where(f => f.IsBackingField).Any());
        }

        [TestMethod]
        public void BasicClassSpecHasMultipleMethods_Test()
        {
            Assert.IsTrue(_basicClassSpec.Methods.Any());
        }

        [TestMethod]
        public void BasicClassSpecHasTwoConstructors_Test()
        {
            Assert.AreEqual(2, _basicClassSpec.Methods.Where(m => m.IsConstructor).Count());
        }

        [TestMethod]
        public void BasicClassSpecHasOneParameterLessConstructor_Test()
        {
            Assert.AreEqual(1, _basicClassSpec.Methods.Where(m => m.IsConstructor && !m.Parameters.Any()).Count());
        }

        [TestMethod]
        public void BasicClassSpecHasOneConstructorWithParameters_Test()
        {
            Assert.AreEqual(1, _basicClassSpec.Methods.Where(m => m.IsConstructor && m.Parameters.Any()).Count());
        }

        [TestMethod]
        public void BasicClassSpecHasMultipleNonPropertyMethods_Test()
        {
            var propertyMethods = _basicClassSpec.Properties.SelectMany(c => c.InnerSpecs());
            var constructors = _basicClassSpec.Methods.Where(m => m.IsConstructor);
            var eventMethods = _basicClassSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _basicClassSpec.Methods.Except(constructors).Except(propertyMethods).Except(eventMethods);

            Assert.IsTrue(nonPropertyMethods.Any());
        }

        [TestMethod]
        public void BasicClassSpecHasParameterlessMethods_Test()
        {
            var propertyMethods = _basicClassSpec.Properties.SelectMany(c => c.InnerSpecs());
            var eventMethods = _basicClassSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _basicClassSpec.Methods.Except(propertyMethods).Except(eventMethods);

            Assert.IsTrue(nonPropertyMethods.Where(d => !d.IsConstructor && !d.Parameters.Any()).Any());
        }

        [TestMethod]
        public void BasicClassSpecHasMultipleParameteredMethods_Test()
        {
            var propertyMethods = _basicClassSpec.Properties.SelectMany(c => c.InnerSpecs());
            var eventMethods = _basicClassSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _basicClassSpec.Methods.Except(propertyMethods).Except(eventMethods);

            Assert.IsTrue(nonPropertyMethods.Where(d => !d.IsConstructor && d.Parameters.Any()).Any());
        }

        [TestMethod]
        public void BasicClassSpecParameteredMethodHasTwoParameters_Test()
        {
            var propertyMethods = _basicClassSpec.Properties.SelectMany(c => c.InnerSpecs());
            var eventMethods = _basicClassSpec.Events.SelectMany(c => c.InnerSpecs());
            var nonPropertyMethods = _basicClassSpec.Methods.Except(propertyMethods).Except(eventMethods);
            var parameteredMethod = nonPropertyMethods.Single(d => d.Name == "PublicMethodWithParameters" && !d.IsConstructor && d.Parameters.Any());

            Assert.AreEqual(2, parameteredMethod.Parameters.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneEvent_Test()
        {
            Assert.AreEqual(1, _basicClassSpec.Events.Length);
        }

        [TestMethod]
        public void BasicClassSpecHasExactlyOneEventField_Test()
        {
            Assert.AreEqual(1, _basicClassSpec.Fields.Where(f => f.IsEventField).Count());
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
            //Assert.AreEqual(1, _basicAttribute.DecoratorForSpecs.OfType<TypeSpec>().Count());
        }

        #endregion

        #region Basic Delegate Tests

        [TestMethod]
        public void BasicDelegateIsDelegateForBasicClassType_Test()
        {
            Assert.AreEqual(1, _basicDelegate.DelegateForSpecs.Where(d => d.DeclaringType.Name == "BasicClass").Count());
        }

        #endregion

        //[TestCleanup]
        //public override void Cleanup()
        //{
        //    var specErrors = _specManager.Faults;
        //    foreach (var fault in specErrors.Where(f => f.Severity == FaultSeverity.Error))
        //    {
        //        fault.ToString();
        //    }
        //    foreach (var fault in specErrors.Where(f => f.Severity == FaultSeverity.Warning))
        //    {
        //        fault.ToString();
        //    }
        //    foreach (var module in _specManager.Modules)
        //    {
        //        Console.WriteLine($"{module.Key}: {module.Value.TypeSpecs.Length}");
        //    }
        //}
    }
}