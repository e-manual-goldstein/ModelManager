using ModelManager.Core;
using ModelManager.Types;
using ModelManager.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StaticCodeAnalysis;
using StaticCodeAnalysis.CodeStructures;
using StaticCodeAnalysis.Types;
using StaticCodeAnalysis.CodeComparer;

namespace ModelManager.Tabs
{
	[MemberOrder(3)]
	public class CodebaseTab : AbstractServiceTab
	{
		public override string Title
		{
			get
			{
				return "Codebase";
			}
		}

		

		public Dictionary<string, IEnumerable<string>> SingleFileTest(string filePath)
		{
			var codeFile = CodeUtils.ReadCodeFile(filePath);
			var dictionary = new Dictionary<string, IEnumerable<string>>();
			//var allFilePaths = CodeUtils.GetAllCodeFilesForProject(@"..\..\ModelManager.csproj");
			var members = codeFile.Classes.SelectMany(c => c.Members);
			dictionary.Add("Name", members.Select(m => m.Name));
			dictionary.Add("ReturnType", members.Select(m => m.ReturnType));
			dictionary.Add("Owner", members.Select(m => m.Owner.Name));
			return dictionary;
		}


		public Dictionary<string, IEnumerable<string>> SingleFileReport(string filePath)
		{
			var codeFile = CodeUtils.ReadCodeFile(filePath);
			var dictionary = new Dictionary<string, IEnumerable<string>>();
			//var allFilePaths = CodeUtils.GetAllCodeFilesForProject(@"..\..\ModelManager.csproj");
			var members = codeFile.Classes.SelectMany(c => c.Members);
			dictionary.Add("Name", members.Select(m => m.Name));
			dictionary.Add("ReturnType", members.Select(m => m.ReturnType));
			dictionary.Add("LineCount", members.Select(m => m.LineCount.ToString()));
			return dictionary;
		}

		private List<string> AltTestC2()
		{
			var allFilePaths = CodeUtils.GetAllCodeFilesForProject(@"..\..\ModelManager.csproj");
			var codeFiles = new List<CodeFile>();
			var classes = new List<ClassDefinition>();
			foreach (var filePath in allFilePaths)
			{
				var codeFile = CodeUtils.ReadCodeFile(filePath);
				codeFiles.Add(codeFile);
			}
			return codeFiles.Select(cf => cf.CondensedContent).ToList();
		}

	    public Dictionary<string, IEnumerable<string>> TestReport()
		{
			var dictionary = new Dictionary<string, IEnumerable<string>>();
			var allFilePaths = CodeUtils.GetAllCodeFilesForProject(@"..\..\ModelManager.csproj");
			var codeFiles = new List<CodeFile>();
			foreach (var filePath in allFilePaths)
			{
				var codeFile = CodeUtils.ReadCodeFile(filePath);
				CodeUtils.FileCount++;
				codeFiles.Add(codeFile);
			}
			var classes = codeFiles.SelectMany(cf => cf.Classes);
			dictionary.Add("ClassName", classes.Select(c => c.Name));
			dictionary.Add("Methods", classes.Select(c => c.Methods.Count.ToString()));
			dictionary.Add("Constructors", classes.Select(c => c.Constructors.Count.ToString()));
			dictionary.Add("Properties", classes.Select(c => c.Properties.Count.ToString()));
			dictionary.Add("Fields", classes.Select(c => c.Fields.Count.ToString()));
			dictionary.Add("NestedTypes", classes.Select(c => c.NestedTypes.Count.ToString()));
			return dictionary;
		}
		
		public List<string> CompareFiles()
		{
			var sourceFilePath = @"..\..\..\ModelManager.UnitTest\SourceCodeFile.cs";
			var targetFilePath = @"..\..\..\ModelManager.UnitTest\TargetCodeFile.cs";
			var sourceFile = CodeUtils.ReadCodeFile(sourceFilePath);
			var targetFile = CodeUtils.ReadCodeFile(targetFilePath);
			var comparer = new CodeFileComparer(sourceFile, targetFile, null);
			return comparer.GetDifferences().Select(d => d.Description()).ToList();
		}

        public Dictionary<string, IEnumerable<string>> ShowComparisonAsTable()
        {
            var dictionary = new Dictionary<string, IEnumerable<string>>();
            var sourceFilePath = @"..\..\..\ModelManager.UnitTest\SourceCodeFile.cs";
            var targetFilePath = @"..\..\..\ModelManager.UnitTest\TargetCodeFile.cs";
            var sourceFile = CodeUtils.ReadCodeFile(sourceFilePath);
            var targetFile = CodeUtils.ReadCodeFile(targetFilePath);
            var comparer = new CodeFileComparer(sourceFile, targetFile, null);
            var differences = comparer.GetDifferences();
            dictionary.Add("Element", differences.Select(d => d.ElementName).ToList());
            dictionary.Add("Feature", differences.Select(d => d.Feature).ToList());
            dictionary.Add("Source Value", differences.Select(d => d.SourceValue).ToList());
            dictionary.Add("Target Value", differences.Select(d => d.TargetValue).ToList());
            return dictionary;
        }

        #region Old Actions

        private Dictionary<string, IEnumerable<string>> PropertyList()
		{
			var dictionary = new Dictionary<string, IEnumerable<string>>();
			var allFilePaths = CodeUtils.GetAllCodeFilesForProject(@"..\..\ModelManager.csproj");
			var codeFiles = new List<CodeFile>();
			int iteration = 0;
			foreach (var filePath in allFilePaths)
			{
				var codeFile = CodeUtils.ReadCodeFile(filePath);
				CodeUtils.FileCount++;
				codeFiles.Add(codeFile);
				iteration++;
			}
			var members = codeFiles.SelectMany(cf => cf.Classes.SelectMany(c => c.Members));
			dictionary.Add("Name", members.Select(m => m.Name));
			dictionary.Add("ReturnType", members.Select(m => m.ReturnType));
			dictionary.Add("Owner", members.Select(m => m.Owner.Name));
			return dictionary;
		}

		private Dictionary<string, IEnumerable<string>> MethodList()
		{
			var dictionary = new Dictionary<string, IEnumerable<string>>();
			var allFilePaths = CodeUtils.GetAllCodeFilesForProject(@"..\..\ModelManager.csproj");
			var codeFiles = new List<CodeFile>();
			int iteration = 0;
			foreach (var filePath in allFilePaths)
			{
				var codeFile = CodeUtils.ReadCodeFile(filePath);
				CodeUtils.FileCount++;
				codeFiles.Add(codeFile);
				iteration++;
			}
			var members = codeFiles.SelectMany(cf => cf.Classes.SelectMany(c => c.Methods));
			dictionary.Add("Name", members.Select(m => m.Name));
			dictionary.Add("ReturnType", members.Select(m => m.ReturnType));
			dictionary.Add("LineCount", members.Select(m => m.LineCount.ToString()));
			dictionary.Add("Owner", members.Select(m => m.Owner.Name));
			return dictionary;
		}

		#endregion
	}
}



