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
        ModuleSpec _vbModuleSpec;
        TypeSpec _basicVBClassSpec;
        TypeSpec _basicSubClassSpec;

        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
            var vbFilePath = "..\\..\\..\\..\\AssemblyAnalyser.VBTestData\\bin\\Debug\\net35\\AssemblyAnalyser.VBTestData.dll";
            _vbModuleSpec = _specManager.LoadModuleSpecFromPath(Path.GetFullPath(vbFilePath));
            _vbModuleSpec.Process();
            _basicVBClassSpec = _vbModuleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.VBTestData.Basics.BasicVBClass");
            _basicSubClassSpec = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.BasicSubClass");
        }

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

        [TestMethod]
        public void InheritedMethodRepresentedBySameSpec_Test()
        {
            var publicMethodSpecs = _specManager.MethodSpecs.Where(p => p.Name == "MethodWithUniqueName");

            Assert.IsTrue(publicMethodSpecs.Any());
            Assert.AreEqual(1, publicMethodSpecs.Count());

            var publicMethodSpec = publicMethodSpecs.Single();

            var inheritedMethod = _basicSubClassSpec.GetAllMethodSpecs().Where(m => m.Name == "MethodWithUniqueName").Single();
            var baseMethod = _basicClassSpec.GetMethodSpecs("MethodWithUniqueName").Single();

            Assert.AreEqual(inheritedMethod, publicMethodSpec);
            Assert.AreEqual(inheritedMethod, baseMethod);

        }

        #endregion

        #region Method Overloading Tests

        [TestMethod]
        //This is a test specifically for Types defined with Method members whose names do not match those of the implemented interfaces
        //This feature appears to only be possible in VisualBasic and not C#
        public void BasicMethodWithAlternateNameHasOverride_Test()
        {
            var alternateNamedFunction = _basicVBClassSpec.Methods.Where(p => p.Name == "AlternateNamedFunction").Single();

            Assert.IsNotNull(alternateNamedFunction.Overrides);
            Assert.IsTrue(alternateNamedFunction.Overrides.Any());
        }

        [TestMethod]
        //This is a test specifically for Types defined with Method members whose names do not match those of the implemented interfaces
        //This feature appears to only be possible in VisualBasic and not C#
        public void BasicMethodWithAlternateNameMatchesInterfaceImplementation_Test()
        {
            var alternateNamedFunction = _basicVBClassSpec.Methods.Where(p => p.Name == "AlternateNamedFunction").Single();
            alternateNamedFunction.DeclaringType.ForceRebuildSpec();

            Assert.IsNotNull(alternateNamedFunction.Implements);
            
        }

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
                var methodSpec = _basicClassSpec.MatchMethodSpecByNameAndParameterType(overload.Name, overload.Parameters, overload.GenericTypeParameters);
                Assert.IsNotNull(methodSpec);
            }
        }


        #endregion

    }
}