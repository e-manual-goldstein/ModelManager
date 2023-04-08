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
    public class InternalDependencyTypeTests : AbstractSpecTests
    {
        const string NAMESPACE = "AssemblyAnalyser.TestData.WithInternalDependencies";

        const string METHOD_BODY = "ClassWithMethodBodyDependency";
        const string PARAMETER = "ClassWithMethodParameterDependency";
        const string RETURN_TYPE = "ClassWithMethodReturnTypeDependency";
        const string GENERIC_TYPE = "ClassWithMethodGenericTypeParameterDependency";
        const string INTERFACE = "ClassWithInterfaceDependency";
        const string PROPERTY = "ClassWithPropertyDependency";
        const string FIELD = "ClassWithFieldDependency";
        const string EVENT = "ClassWithEventDependency";

        private string[] CLASS_NAMES = new[] { METHOD_BODY, PARAMETER, RETURN_TYPE, GENERIC_TYPE, INTERFACE, PROPERTY, FIELD, EVENT };

        #region Dependency Tests

        [TestMethod]
        public void DependencyClassSpecsAreNotNull_Test()
        {
            foreach (var className in CLASS_NAMES)
            {
                var classSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{className}");
                Assert.IsNotNull(classSpec);
            }            
        }

        [TestMethod] //Dependencies have not been finalised
        public void DependencyClassSpecRegistersMethodBodyDependency_Test()
        {
            //var classSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{METHOD_BODY}");
            //classSpec.ForceRebuildSpec();

            //var basicClassMethodDependencies = _basicClassSpec.DependsOn
            //    .OfType<MethodToTypeDependency>();

            //Assert.IsTrue(basicClassMethodDependencies.Select(d => d.DependsOn).Contains(classSpec));
            //Assert.IsNotNull(classSpec);            
        }
        
        #endregion

    }
}