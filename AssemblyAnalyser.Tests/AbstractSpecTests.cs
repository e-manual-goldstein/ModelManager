using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace AssemblyAnalyser.Tests
{
    public abstract class AbstractSpecTests
    {
        protected ISpecManager _specManager;
        protected ILoggerProvider _loggerProvider;
        protected IExceptionManager _exceptionManager;
        protected ModuleSpec _moduleSpec;
        protected TypeSpec _basicClassSpec;

        [TestInitialize]
        public virtual void Initialize()
        {
            _exceptionManager = new ExceptionManager();
            _loggerProvider = NSubstitute.Substitute.For<ILoggerProvider>();
            _specManager = new SpecManager(_loggerProvider, _exceptionManager);
            var filePath = "..\\..\\..\\..\\AssemblyAnalyser.TestData\\bin\\Debug\\net6.0\\AssemblyAnalyser.TestData.dll";
            _moduleSpec = _specManager.LoadModuleSpecFromPath(Path.GetFullPath(filePath));
            _moduleSpec.Process();
            _specManager.ProcessSpecs(_moduleSpec.TypeSpecs, false);
            _basicClassSpec = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.BasicClass");
        }

        [TestCleanup]
        public virtual void Cleanup()
        {
            var specErrors = _specManager.Faults;
            foreach (var fault in specErrors.Where(f => f.Severity == FaultSeverity.Error))
            {
                fault.ToString();
            }
            foreach (var fault in specErrors.Where(f => f.Severity == FaultSeverity.Warning))
            {
                fault.ToString();
            }
            foreach (var type in _specManager.TypeSpecs)
            {
                Console.WriteLine($"{type}: {type.Module}");
            }
        }

    }
}