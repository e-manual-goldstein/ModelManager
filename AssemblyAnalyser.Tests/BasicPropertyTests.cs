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
    public class BasicPropertyTests
    {
        ISpecManager _specManager;
        ILoggerProvider _loggerProvider;
        IExceptionManager _exceptionManager;
        ModuleSpec _moduleSpec;
        TypeSpec _basicClassSpec;
        TypeSpec _basicInterfaceSpec;
        TypeSpec _basicAttribute;
        PropertySpec _basicPropertySpec;

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
            _basicAttribute = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.BasicAttribute");
        }

        #region Basic Property Tests
        
        [TestMethod]
        public void BasicPropertySpecIsNotNull_Test()
        {
            Assert.IsNotNull(_basicClassSpec.GetPropertySpec("PublicProperty"));
        }

        [TestMethod]
        public void BasicPropertySpecLinkedToInterfaceImplementationMember_Test()
        {
            var interfaceImplementation = _basicClassSpec.GetPropertySpec("ReadOnlyInterfaceImpl");

            Assert.IsNotNull(interfaceImplementation.Implements);
        }

        #endregion

    }
}