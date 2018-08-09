using StaticCodeAnalysis.CodeStructures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.Types
{
	public class CodeFile : IStaticCodeElement
	{
		public const string preProcessingTag = "#";
		public CodeFile()
		{
            UsingDeclarations = new List<string>();
			CodeRegions = new List<string>();
            CodeBlocks = new List<CodeBlock>();
			GlobalTypes = new List<TypeDefinition>();
			Namespaces = new List<NamespaceDefinition>();
			Elements = new List<IStaticCodeElement>();
		}

		public string Name { get; set; }

		public bool OmittedFromAnalysis { get; set; }

        private string _condensedContent;

		public IStaticCodeElement Owner { get; set; }

		public List<IStaticCodeElement> Elements { get; set; }

        public string CondensedContent
        {
            get => _condensedContent;
        }

		public string Content { get; set; }

        public List<CodeBlock> CodeBlocks { get; set; }

		public List<string> UsingDeclarations { get; set; }

		public List<string> CodeRegions { get; set; }

		public List<NamespaceDefinition> Namespaces { get; set; }

		public List<TypeDefinition> GlobalTypes { get; set; }

		public List<ClassDefinition> Classes
		{
			get
			{
				return Namespaces.SelectMany(n => n.Classes).ToList();
			}
		}

		public List<TypeDefinition> DefinedTypes
		{
			get
			{
				return Namespaces.SelectMany(n => n.DefinedTypes).ToList();
			}
		}

        public string GetNamespaceDefinitions(string content)
        {
			var newContent = content;
			newContent = CodeUtils.RemoveComments(newContent);
            var namespaceMatches = Regex.Matches(newContent, @"namespace(?'Namespace'[\w\s\.]*)" + CodeUtils.CodeBlockTagPattern, RegexOptions.ExplicitCapture) as ICollection;
            foreach (Match match in namespaceMatches)
            {
                var namespaceName = match.Groups["Namespace"].Value.Trim();
                var fullBlock = CodeUtils.ExpandCodeBlock(match.Groups["BlockId"].Value);
				fullBlock = getUsingDeclarations(fullBlock);
                var nsDefinition = new NamespaceDefinition(namespaceName, fullBlock, this);
				Namespaces.Add(nsDefinition);
				newContent = newContent.Replace(match.Value, "");
            }
			return newContent;
        }

		public string GetGlobalTypes(string content)
		{
			var newContent = content;
			newContent = CodeUtils.RemoveComments(newContent);
			var elementDefinitions = CodeUtils.GetElementDefinitions(newContent, this);
			var typeDefinitions = elementDefinitions.OfType<TypeDefinition>().ToList();
			if (elementDefinitions.Except(typeDefinitions).Any())
				throw new CodeParseException("Unrecognised Elements discovered in CodeFile");
			GlobalTypes = typeDefinitions;
			return newContent;
		}
		
		public override string ToString()
        {
            return "Code File: " + Name;
        }

		#region Alternative Mode

		public string GetCodeBlocks(string content)
		{
            var codeBlocks = new List<CodeBlock>();
            var declaredStrings = new List<CodeString>();
            var parenthesisBlocks = new List<ParenthesisBlock>();
			var newContent = content;
			newContent = clearEmptyCodeBlocks(codeBlocks, newContent);
			newContent = Operator.SymboliseBlock(newContent, this);
			newContent = getUsingDeclarations(newContent);
            _condensedContent = CodeUtils.GetCodeBlocks(codeBlocks, newContent, true);
            CodeBlocks = codeBlocks;
			return _condensedContent;
		}

		private string clearEmptyCodeBlocks(List<CodeBlock> codeBlocks, string content)
        {
            var emptyPattern = @"({\s*})";
            var emptyBlockMatches = Regex.Matches(content, emptyPattern);
            foreach (Match match in emptyBlockMatches)
            {
                var blockText = match.Value;
                if (!string.IsNullOrEmpty(blockText))
                {
                    var newBlock = new CodeBlock(blockText, true);
                    codeBlocks.Add(newBlock);
                    content = content.Replace(blockText, @"BLOCK#" + newBlock.CodeBlockId + "#");
                }
            }
            return content;
        }

        public string ClearDeclaredStrings(List<CodeString> declaredStrings, string content)
        {
            //var allStringsPattern = @"[^""]""(?!"")(?'InnerString'[^""]+)""[^""]";
            var allStringsPattern = @"(@""(?:[^""]|"""")*""|""(?:\\.|[^\\""])*"")";
            var stringMatches = Regex.Matches(content, allStringsPattern);
            foreach (Match match in stringMatches)
            {
                var newString = new CodeString(match.Value, this);
                declaredStrings.Add(newString);
                content = content.Replace(match.Value, @"STRING#" + newString.CodeStringId + "#");
            }
            return content;
        }

        public string getUsingDeclarations(string content)
        {
			var newContent = content;
            var usingMatches = Regex.Matches(newContent, @"(^|\W)(using\s+(?'UsingDeclaration'[\w\._\=\s]+));");
            foreach (Match usingMatch in usingMatches)
            {
                var usingDeclaration = usingMatch.Groups["UsingDeclaration"].Value;
				if (string.IsNullOrWhiteSpace(usingDeclaration))
					throw new CodeParseException("Could not parse 'Using' declaration");
                UsingDeclarations.Add(usingDeclaration);
				newContent = newContent.Replace(usingMatch.Value, "");
            }
            return newContent;
        }

		public bool HasPreprocessing()
		{
			foreach (var directive in CodeUtils.PreProcessorLookup())
			{
				bool handled = directive.Value;
				if (handled)
					continue;
				if (Content.Contains(directive.Key))
					return true;
			}
			return false;
		}

		public string HandlePreProcessing(string contents)
		{
			return extractRegions(contents);
		}

		private string extractRegions(string contents)
		{
			var regionPattern = preProcessingTag + @"\s*region[^\r\n\w]*(?'RegionName'[^\r\n]*)";
			var endRegionPattern = preProcessingTag + @"\s*endregion[^\r\n]*";
			var regionMatches = Regex.Matches(contents, regionPattern);
			foreach (Match regionMatch in regionMatches)
			{
				var regionName = regionMatch.Groups["RegionName"].Value;
				if (string.IsNullOrWhiteSpace(regionName))
					regionName = "Unnamed Region";
				CodeRegions.Add(regionName);
				contents = Regex.Replace(contents, regionPattern, "");
			}
			return Regex.Replace(contents, endRegionPattern, "");
		}

		#endregion
	}
}
