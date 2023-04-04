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
        ModuleSpec _vbModuleSpec;
        TypeSpec _basicClassSpec;
        TypeSpec _basicVBClassSpec;

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
            var vbFilePath = "..\\..\\..\\..\\AssemblyAnalyser.VBTestData\\bin\\Debug\\net35\\AssemblyAnalyser.VBTestData.dll";
            var vbModule = Mono.Cecil.ModuleDefinition.ReadModule(Path.GetFullPath(vbFilePath));
            _vbModuleSpec = _specManager.LoadModuleSpec(vbModule);
            _vbModuleSpec.Process();
            //_specManager.ProcessLoadedProperties();
            _basicClassSpec = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.BasicClass");
            _basicVBClassSpec = _vbModuleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.VBTestData.Basics.BasicVBClass");
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
            _basicClassSpec.ForceRebuildSpec();
            var interfaceImplementation = _basicClassSpec.GetPropertySpec("ReadOnlyInterfaceImpl");
            Assert.IsNotNull(interfaceImplementation.Implements);
        }

        [TestMethod]
        public void BasicPropertyWithArrayTypeHasOwnSpec_Test()
        {
            var stringProperty = _basicClassSpec.GetPropertySpec("PublicProperty");
            var stringArrayProperty = _basicClassSpec.GetPropertySpec("ArrayProperty");

            var stringSpec = stringProperty.PropertyType;
            var stringArraySpec = stringArrayProperty.PropertyType;

            Assert.AreNotSame(stringSpec, stringArraySpec);
        }

        [TestMethod]
        //This is a test specifically for Types defined with Property members that have parameters
        //This feature appears to only be possible in VisualBasic and not C#
        public void BasicPropertyCanHaveOverloads_Test()
        {
            //Assert there are two identically named properties
            Assert.AreEqual(2, _basicVBClassSpec.Properties.Where(p => p.Name == "PropertyWithParameters").Count());
            foreach (var property in _basicVBClassSpec.Properties)
            {
                var propertySpec = _basicVBClassSpec
                    .MatchPropertySpecByNameAndParameterType(property.Name, property.Parameters);
                Assert.IsNotNull(propertySpec);
            }            
        }
        #endregion

    }
}