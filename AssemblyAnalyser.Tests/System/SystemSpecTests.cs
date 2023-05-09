using AssemblyAnalyser.Specs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Tests.System
{
    [TestClass]
    public class SystemSpecTests : AbstractSpecTests
    {
        [TestMethod]
        public void EnsureOnlyOneSpecExistsForSystemObjectType_Test()
        {
            var allAssemblies = ((SpecManager)_specManager).AssemblySpecs.Values;
            _specManager.ProcessSpecs(allAssemblies);

            var allModules = allAssemblies.SelectMany(a => a.Modules).ToArray();
            _specManager.ProcessSpecs(allModules);

            var systemModule = allModules
                .Single(m => m.ModuleShortName == SystemAssemblySpec.SYSTEM_MODULE_NAME);
            systemModule.ForceRebuildSpec();

            var allTypes = allModules.SelectMany(m => m.TypeSpecs).ToArray();
            _specManager.ProcessSpecs(allTypes);
            var systemObjectTypes = allTypes.Where(t => t.FullTypeName == "System.Object");

            Assert.IsTrue(systemObjectTypes.Any());
            Assert.AreEqual(1, systemObjectTypes.Count());
        }

    }
}
