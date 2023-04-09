using AssemblyAnalyser.AssemblyLocators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Tests.Locators
{
    [TestClass]
    public class VersionPickerTests
    {
        string[] versions = new string[] { "1.0.0", "2.5.4", "2.5.6", "0.5.7", "13.5.4", "4.5.1", "4.6.0" };

        [TestMethod]
        public void PickCorrectMatchingMajorVersion_Test()
        {
            var majorVersion = "13.5.4";

            var bestMatchVersion = VersionPicker.PickBestVersion(versions, majorVersion);

            Assert.AreEqual(majorVersion, bestMatchVersion);
        }

        [TestMethod]
        public void PickCorrectNextBestMajorVersion_Test()
        {
            var majorVersion = "3.0.0";

            var bestMatchVersion = VersionPicker.PickBestVersion(versions, majorVersion);

            Assert.AreEqual("4.5.1", bestMatchVersion);
        }

        [TestMethod]
        public void CanFindVersionForEachMajorVersion_Test()
        {
            for (int i = 0; i < 15; i++)
            {
                for (int j = 0; j < 15; j++)
                {
                    for (int k = 0; k < 15; k++)
                    {
                        var version = $"{i}.{j}.{k}";
                        try
                        {
                            var bestMatchVersion = VersionPicker.PickBestVersion(versions, version);
                            Console.WriteLine($"{version} Best Match: {bestMatchVersion}");
                        }
                        catch
                        {

                        }

                    }
                }
            }            
        }
    }
}
