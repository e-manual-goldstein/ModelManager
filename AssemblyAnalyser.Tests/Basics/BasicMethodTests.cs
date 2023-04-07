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
    public class BasicMethodTests : AbstractSpecTests
    {
        #region Basic Method Tests
        
        [TestMethod]
        public void BasicMethodSpecIsNotNull_Test()
        {
            Assert.IsNotNull(_basicClassSpec.GetMethodSpecs("PublicMethod").SingleOrDefault());
        }

        [TestMethod]
        public void BasicMethodSpecHasReturnTypeSpec_Test()
        {
            var method = _basicClassSpec.GetMethodSpecs("PublicMethod").SingleOrDefault();
            Assert.IsNotNull(method.ReturnType);
        }

        [TestMethod]
        public void BasicMethodSpecLinkedToInterfaceImplementationMember_Test()
        {
            var interfaceImplementation = _basicClassSpec.GetMethodSpecs("PublicMethod").SingleOrDefault();
            Assert.IsNotNull(interfaceImplementation.Implements);
        }

        [TestMethod]
        public void BasicMethodWithOutParameterHasCorrectParameters()
        {
            var methodWithOutParameter = _basicClassSpec.GetMethodSpecs("MethodWithOutParameter").SingleOrDefault();
            methodWithOutParameter.ForceRebuildSpec();
            Assert.AreEqual(3, methodWithOutParameter.Parameters.Length);
            Assert.AreEqual(1, methodWithOutParameter.Parameters.Where(m => m.IsOut).Count());
        }

        #endregion

        #region Method Overloading Tests

        [TestMethod]
        public void BasicMethodHasFourOverloads_Test()
        {
            Assert.AreEqual(4, _basicClassSpec.GetMethodSpecs("OverloadedMethod").Length);
        }

        [TestMethod]
        public void BasicMethodCanDistinguishAllOverloads_Test()
        {
            var overloadedMethods = _basicClassSpec.GetMethodSpecs("OverloadedMethod");
            foreach (var overload in overloadedMethods)
            {
                var methodSpec = _basicClassSpec.MatchMethodSpecByNameAndParameterType(overload.Name, overload.Parameters, overload.GenericTypeArguments);
                Assert.IsNotNull(methodSpec);
            }
        }


        #endregion

    }
}