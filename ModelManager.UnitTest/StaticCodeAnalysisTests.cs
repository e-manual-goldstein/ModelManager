using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StaticCodeAnalysis;
using StaticCodeAnalysis.Types;

namespace ModelManager.UnitTest
{

    [TestClass]
    public class StaticCodeAnalysisTests
    {
        public const string TestFilePath = @"..\..\SourceCodeFile.cs";

        [TestMethod]
        public void ClearStringsTest()
        {

        }

        [TestMethod]
        public void ReadCodeFile_Test()
        {
            var codeFile = CodeUtils.ReadCodeFile(@"..\..\TestCodeFile.cs");
            Assert.IsTrue(codeFile != null, "Method ReadCodeFile must produce a new CodeFile object");
        }

        [TestMethod]
        public void GetAllCodeFilesForProject_Test()
        {
            var allFilePaths = CodeUtils.GetAllCodeFilesForProject(@"..\..\..\ModelManager\ModelManager.csproj");
            Assert.IsTrue(allFilePaths != null);
        }

        [TestMethod]
        public void ReadProjectFile_Test()
        {
            var codeProjectFile = new CodeProjectFile(@"..\..\ModelManager.UnitTest.csproj", false);
            Assert.IsNotNull(codeProjectFile);
        }

        [TestMethod]
        public void AnalyseProjectFile_Test()
        {
            var codeProjectFile = new CodeProjectFile(@"..\..\ModelManager.UnitTest.csproj", true);
            Assert.IsNotNull(codeProjectFile);
        }

        [TestMethod]
        public void ReadAllCodeFilesForProject_Test()
        {
            var allFilePaths = CodeUtils.GetAllCodeFilesForProject(@"..\..\..\ModelManager\ModelManager.csproj");
            foreach (var path in allFilePaths)
            {
                Assert.IsNotNull(CodeUtils.ReadCodeFile(path));
            }
        }

        [TestMethod]
        public void ReadAllCodeFilesForSolution_Test()
        {
            var allFilePaths = CodeUtils.GetAllCodeFilesForSolution(@"..\..\..\ModelManager.sln");
            foreach (var path in allFilePaths)
            {
                Assert.IsNotNull(CodeUtils.ReadCodeFile(path));
            }
        }

        [TestMethod]
        public void GetAllCodeProjectFilesForSolution_Test()
        {
            var allFilePaths = CodeUtils.GetAllCodeProjectFilesForSolution(@"..\..\..\ModelManager.sln");
            foreach (var path in allFilePaths)
            {
                var codeProjectFile = new CodeProjectFile(path, false);
            }
        }

        [TestMethod]
        public void AnalyseAllCodeProjectFilesForSolution_Test()
        {
            var allFilePaths = CodeUtils.GetAllCodeProjectFilesForSolution(@"..\..\..\ModelManager.sln");
            foreach (var path in allFilePaths)
            {
                var codeProjectFile = new CodeProjectFile(path, true);
            }
        }

        [TestMethod]
        public void CreateCodeFileFromContents_Test()
        {
            if (!File.Exists(TestFilePath))
                throw new FileNotFoundException("Test code file not found");
            var sourceContents = File.ReadAllText(TestFilePath);
            var codeFile = CodeUtils.CreateCodeFileFromContents(sourceContents, "test", TestFilePath);
            Assert.IsTrue(codeFile != null, "Method CreateCodeFileFromContents must produce a new CodeFile object");
        }
    }
}

