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
    public class BasicMethodTests
    {
        ISpecManager _specManager;
        ILoggerProvider _loggerProvider;
        IExceptionManager _exceptionManager;
        ModuleSpec _moduleSpec;
        TypeSpec _basicClassSpec;
        TypeSpec _basicInterfaceSpec;

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
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.BasicClass");
            _basicInterfaceSpec = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.IBasicInterface");
        }

        #region Basic Method Tests
        
        [TestMethod]
        public void BasicMethodSpecIsNotNull_Test()
        {
            Assert.IsNotNull(_basicClassSpec.GetMethodSpecs("PublicMethod").SingleOrDefault());
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
                var methodSpec = _basicClassSpec.MatchMethodSpecByNameAndParameterType(overload.Name, overload.Parameters);
                Assert.IsNotNull(methodSpec);
            }
        }


        #endregion

    }
}