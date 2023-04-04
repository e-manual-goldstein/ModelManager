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
    public class BasicFieldTests
    {
        ISpecManager _specManager;
        ILoggerProvider _loggerProvider;
        IExceptionManager _exceptionManager;
        ModuleSpec _moduleSpec;
        TypeSpec _basicClassSpec;

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
            //_specManager.ProcessLoadedMethods();
            //_specManager.ProcessLoadedFields();
            //_specManager.ProcessLoadedParameters();
            //_specManager.ProcessLoadedEvents();
            //_specManager.ProcessLoadedAttributes();
            _basicClassSpec = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.BasicClass");
        }

        #region Basic Field Tests
        
        //[TestMethod]
        //public void BasicFieldSpecIsNotNull_Test()
        //{
        //    Assert.IsNotNull(_basicClassSpec.GetFieldSpec("PublicField"));
        //}

        //[TestMethod]
        //public void BasicFieldWithArrayTypeHasOwnSpec_Test()
        //{
        //    var stringField = _basicClassSpec.GetFieldSpec("PublicField");
        //    var stringArrayField = _basicClassSpec.GetFieldSpec("ArrayField");

        //    var stringSpec = stringField.FieldType;
        //    var stringArraySpec = stringArrayField.FieldType;

        //    Assert.AreNotSame(stringSpec, stringArraySpec);
        //}

        #endregion

    }
}