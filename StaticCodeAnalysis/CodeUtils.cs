using StaticCodeAnalysis.CodeComparer;
using StaticCodeAnalysis.CodeStructures;
using StaticCodeAnalysis.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StaticCodeAnalysis
{
	public static class CodeUtils
	{
		private static readonly Lazy<IDictionary<string, Type>> _typeLookup = new Lazy<IDictionary<string, Type>>
		(() => {
			var dictionary = new Dictionary<string, Type>();
			dictionary.Add("interface", typeof(InterfaceDefinition));
			dictionary.Add("class", typeof(ClassDefinition));
			dictionary.Add("enum", typeof(EnumDefinition));
			dictionary.Add("struct", typeof(StructDefinition));
			return dictionary;
		});

		private static readonly Lazy<IDictionary<string, ModifierType>> _modifierLookup = new Lazy<IDictionary<string, ModifierType>>(() =>
		 {
			 var dictionary = new Dictionary<string, ModifierType>();
			 dictionary.Add("abstract", ModifierType.TypeAndMember);
			 dictionary.Add("sealed", ModifierType.Type);
			 dictionary.Add("const", ModifierType.Member);
			 dictionary.Add("event", ModifierType.Member);
			 dictionary.Add("extern", ModifierType.Member);
			 dictionary.Add("override", ModifierType.Member);
			 dictionary.Add("virtual", ModifierType.Member);
			 dictionary.Add("readonly", ModifierType.Member);
			 dictionary.Add("unsafe", ModifierType.Member);
			 dictionary.Add("volatile", ModifierType.Member);
			 dictionary.Add("static", ModifierType.Member);
			 dictionary.Add("new", ModifierType.Member);
			 dictionary.Add("delegate", ModifierType.Member);
			 dictionary.Add("public", ModifierType.TypeAndMemberAccess);
			 dictionary.Add("private", ModifierType.TypeAndMemberAccess);
			 dictionary.Add("protected", ModifierType.TypeAndMemberAccess);
			 dictionary.Add("internal", ModifierType.TypeAndMemberAccess);
			 return dictionary;
		 });

		private static readonly Lazy<IDictionary<string, bool>> _preProcessorLookup = new Lazy<IDictionary<string, bool>>(() =>
		{
			var dictionary = new Dictionary<string, bool>();
			dictionary.Add("#if", false);
			dictionary.Add("#else", false);
			dictionary.Add("#elif", false);
			dictionary.Add("#endif", false);
			dictionary.Add("#define", false);
			dictionary.Add("#undef", false);
			dictionary.Add("#warning", false);
			dictionary.Add("#error", false);
			dictionary.Add("#line", false);
			dictionary.Add("#region", true);
			dictionary.Add("#endregion", true);
			dictionary.Add("#pragma", false);
			dictionary.Add("#pragmawarning", false);
			dictionary.Add("#pragmachecksum", false);
			return dictionary;
		});

		public static IDictionary<string, Type> TypeLookup()
		{
			return _typeLookup.Value;
		}

		public static IDictionary<string, ModifierType> ModifierLookup()
		{
			return _modifierLookup.Value;
		}

		public static IDictionary<string, bool> PreProcessorLookup()
		{
			return _preProcessorLookup.Value;
		}

		public static string CodeBlockPattern = "(((?'Open'{)[^{}]+)+((?'Close-Open'})[^{}]*)+)+?(?(Open)(?!))";
		public static string CodeBlockTagPattern = "BLOCK#(?'BlockId'[0-9]+)#";



		private static int _fileCount = 0;
		public static int FileCount { get => _fileCount; set => _fileCount = value; }

		public static string ExpandAll(string condensedInput)
		{
			throw new NotImplementedException();
		}

		public static List<string> GetAllCodeFilesForProject(string projectFilePath)
		{
			var folderPath = Path.GetDirectoryName(projectFilePath);
			var filePaths = new List<string>();
			if (!File.Exists(projectFilePath))
				throw new FileNotFoundException("File not found");
			var csprojFile = File.ReadAllText(projectFilePath).ToString();
			var fileMatches = Regex.Matches(csprojFile, @"\<Compile Include=""(.*\.cs)""") as ICollection;
			foreach (Match item in fileMatches)
			{
				filePaths.Add(Path.Combine(folderPath, item.Groups[1].Value));
			}
			return filePaths;
		}

        public static List<string> GetAllCodeProjectFilesForSolution(string solutionFilePath)
        {
            var folderPath = Path.GetDirectoryName(solutionFilePath);
            var projectFilePaths = new List<string>();
            if (!File.Exists(solutionFilePath))
                throw new FileNotFoundException("File not found");
            var slnFile = File.ReadAllText(solutionFilePath).ToString();
            var projFileMatches = Regex.Matches(slnFile, @"""(?'ProjectFilePath'[^""]*?\.csproj)""") as ICollection;
            foreach (Match projectFileMatch in projFileMatches)
            {
                var projectFilePath = Path.Combine(folderPath, projectFileMatch.Groups["ProjectFilePath"].Value);
                projectFilePaths.Add(projectFilePath);
            }
            return projectFilePaths;
        }

		public static List<string> GetAllCodeFilesForSolution(string solutionFilePath)
		{
			var folderPath = Path.GetDirectoryName(solutionFilePath);
			var filePaths = new List<string>();
			if (!File.Exists(solutionFilePath))
				throw new FileNotFoundException("File not found");
			var slnFile = File.ReadAllText(solutionFilePath).ToString();
			var projFileMatches = Regex.Matches(slnFile, @"""(?'ProjectFilePath'[^""]*?\.csproj)""") as ICollection;
			foreach (Match projectFilePath in projFileMatches)
			{
				var fullFilePath = Path.Combine(folderPath, projectFilePath.Groups["ProjectFilePath"].Value);
				filePaths.AddRange(GetAllCodeFilesForProject(fullFilePath));
			}
			return filePaths;
		}

		public static string RemoveComments(string codeBlock)
		{
			var lineCommentPattern = @"//.*[\r\n]";
			var newCodeBlock = Regex.Replace(codeBlock, lineCommentPattern, "");
			var extendedCommentPattern = @"/\*.*?\*/";
			newCodeBlock = Regex.Replace(newCodeBlock, extendedCommentPattern, "", RegexOptions.Singleline);
			return newCodeBlock;
		}

		public static void GetExtensions(this IHasExtensions extendingType, string extensions)
		{
			var pattern = @"(\w+)";
			var groups = Regex.Matches(extensions, pattern) as ICollection;
			foreach (Match group in groups)
			{
				extendingType.Extensions.Add(group.Value);
			}
		}

		public static void GetAttributes(this IHasAttributes decoratedElement, string attributeList)
		{
			if (string.IsNullOrWhiteSpace(attributeList))
				return;
			foreach (string attribute in attributeList.Split(','))
			{
				if (string.IsNullOrWhiteSpace(attribute))
					continue;
				var match = Regex.Match(attribute.Trim(), "(?'AttributeName'^[^_]*)_*");
				var attributeName = match.Groups["AttributeName"].Value;
				var parameters = ParenthesisBlock.InnerBlock(attribute.Trim());
				decoratedElement.Attributes.Add(new DeclaredAttribute(attributeName, parameters));
			}
		}

		public static string ExpandCodeBlock(string blockId)
		{
			int id = 0;
			if (!int.TryParse(blockId, out id))
				throw new ArgumentException("Could not parse Block ID from input: " + blockId);
			if (!CodeBlock.CodeBlocks.ContainsKey(id))
				throw new ArgumentException("No matching Code Block found for ID: " + blockId);
			return CodeBlock.CodeBlocks[id].CondensedContent;
		}

		public static string ReloadStrings(string content)
		{
			var stringPattern = "STRING#(?'StringId'[0-9]+)#";
			foreach (Match stringMatch in Regex.Matches(content, stringPattern))
			{
				var stringId = stringMatch.Groups["StringId"].Value;
				content = CodeString.ExpandString(content, stringId);
			}
			return content;
		}

		public static string GetCodeBlocks(List<CodeBlock> codeBlocks, string content)
		{
			var codeBlockMatches = Regex.Matches(content, CodeBlockPattern);
			foreach (Match match in codeBlockMatches)
			{
				var blockText = match.Groups["Close"].Value;
				if (!string.IsNullOrWhiteSpace(blockText))
				{
					var newBlock = new CodeBlock(blockText.Trim());
					codeBlocks.Add(newBlock);
					content = content.Replace("{" + blockText + "}", @"BLOCK#" + newBlock.CodeBlockId + "#");
				}
			}
			return content;
		}

		public static string GetCodeBlocks(List<CodeBlock> codeBlocks, string content, bool alternate)
		{
			var codeBlockPattern = @"{(?'BLOCK'[^\{\}]*)}";
			var newContent = content;
			while (Regex.Match(newContent, codeBlockPattern).Success)
			{
				var matches = Regex.Matches(newContent, codeBlockPattern);
				foreach (Match match in matches)
				{
					var blockDefinition = match.Groups["BLOCK"].Value;
					var codeBlock = new CodeBlock(blockDefinition);
					newContent = newContent.Replace("{" + blockDefinition + "}", @"BLOCK#" + codeBlock.CodeBlockId + "#");
				}
			}
			return newContent;
		}

		[Obsolete]
		public static void GetModifiers(this IHasModifiers modifiedElement, string declarations)
		{
			var modifierPattern = @"([\w\<\>\[\]])*";
			var matches = Regex.Matches(declarations, modifierPattern);
			foreach (Match match in matches)
			{
				if (!string.IsNullOrEmpty(match.Value))
					modifiedElement.Modifiers.Add(match.Value);
			}
		}

		public static List<IStaticCodeElement> GetElementDefinitions(string content, IStaticCodeElement owner = null)
		{
			//TODO: Is this next line irrelevant?
			content = RemoveComments(content);
			//Why Reload the Strings now? Expand them when setting the final property;
			//content = ReloadStrings(content);
			content = ParenthesisBlock.SymboliseBlock(content, owner);
			var elementPattern = @"(?'ElementDeclaration'[""\w\s\:\?\,\[\(\]\)\.\<\>]*)"
									+ "(" + CodeBlockTagPattern + "|;" + "|(?'FieldInit'=[^;]+;))";
			var elementMatches =
				Regex.Matches(content, elementPattern, RegexOptions.Singleline) as ICollection;
			var elementDefinitions = new List<IStaticCodeElement>();
			foreach (Match elementMatch in elementMatches)
			{
				var groups = elementMatch.Groups;
				var elementDeclaration = groups["ElementDeclaration"].Value.Trim();
				var elementContent = !string.IsNullOrWhiteSpace(groups["BlockId"].Value) ?
					ExpandCodeBlock(groups["BlockId"].Value.Trim()) :
					groups["FieldInit"].Value.Trim();
				if (string.IsNullOrEmpty(elementDeclaration) && string.IsNullOrEmpty(elementContent))
					continue;
				var elementDefinition = NewElementFromDeclaration(elementDeclaration, elementContent, owner);
				elementDefinitions.Add(elementDefinition);
			}
			return elementDefinitions;
		}

		private static string getExtensions(string typeDeclaration)
		{
			var pattern = @"\:\s+(.*)";
			return Regex.Match(typeDeclaration, pattern).Groups[1].Value;
		}

		private static string removeDeclaredExtensions(string typeDeclaration)
		{
			var pattern = @"\:\s+(.*)";
			return Regex.Replace(typeDeclaration, pattern, "").Trim();
		}

		private static string getAttributes(string typeDeclaration)
		{
			var pattern = @"\[(?'Attribute'[^\]]*)\]*";
			var attributeList = new StringBuilder();
			var matches = Regex.Matches(typeDeclaration, pattern);
			foreach (Match match in matches)
			{
				attributeList.Append(match.Groups["Attribute"].Value);
				attributeList.Append(",");
			}
			return attributeList.ToString();
		}

		private static string removeDeclaredAttributes(string typeDeclaration)
		{
			var pattern = @"\[(?'Attribute'[^\]]*)\]*";
			return Regex.Replace(typeDeclaration, pattern, "").Trim();
		}

		public static IStaticCodeElement NewElementFromDeclaration(string elementDeclaration, string elementContent, IStaticCodeElement owner = null)
		{
			var inlineElementDeclaration = elementDeclaration.AsSingleLine();
			var typeKeywordPattern = @"(^|\s)(class|struct|interface|enum)\s";
			if (Regex.Match(inlineElementDeclaration, typeKeywordPattern).Success)
				return NewTypeFromDeclaration(inlineElementDeclaration, elementContent, owner);
			return NewTypeMemberFromDeclaration(inlineElementDeclaration, elementContent, owner);
		}

		public static TypeDefinition NewTypeFromDeclaration(string typeDeclaration, string typeContent, IStaticCodeElement owner = null)
		{
			var inlineTypeDeclaration = typeDeclaration.AsSingleLine();
			var extensions = getExtensions(inlineTypeDeclaration);
			var attributes = getAttributes(inlineTypeDeclaration);
			inlineTypeDeclaration = string.IsNullOrEmpty(extensions) ? inlineTypeDeclaration : removeDeclaredExtensions(inlineTypeDeclaration);
			inlineTypeDeclaration = string.IsNullOrEmpty(attributes) ? inlineTypeDeclaration : removeDeclaredAttributes(inlineTypeDeclaration);
			var pattern = @"^(\w*\s+)*(?'TypeKey'\w+\s+)(?'TypeName'(\w+))$";
			var name = Regex.Match(inlineTypeDeclaration, pattern).Groups["TypeName"].Value.Trim();
			var typeKey = Regex.Match(inlineTypeDeclaration, pattern).Groups["TypeKey"].Value.Trim();
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(typeKey))
				return null;
			var type = TypeLookup()[typeKey];
			if (type == null)
				throw new CodeParseException("Type not found");
			if (typeof(IComplexType).IsAssignableFrom(type))
				return Activator.CreateInstance(type, inlineTypeDeclaration, typeContent, extensions, attributes, owner) as TypeDefinition;
			//Don't think there's any Type which can't be decorated with an Attribute. The lines below might be simplified
			if (typeof(IHasAttributes).IsAssignableFrom(type))
				return Activator.CreateInstance(type, inlineTypeDeclaration, typeContent, attributes, owner) as TypeDefinition;
			return Activator.CreateInstance(type, inlineTypeDeclaration, typeContent) as TypeDefinition;
		}

		public static IStaticCodeElement NewTypeMemberFromDeclaration(string elementDeclaration, string elementContent, IStaticCodeElement owner)
		{
			if (string.IsNullOrWhiteSpace(elementDeclaration))
				throw new ArgumentException("Invalid Element Declaration");
			var attributes = getAttributes(elementDeclaration);
			elementDeclaration = string.IsNullOrEmpty(attributes) ? elementDeclaration : removeDeclaredAttributes(elementDeclaration);
			if (Regex.Match(elementContent, "^=.*;$").Success || string.IsNullOrEmpty(elementContent))
				return new FieldDefinition(elementDeclaration, elementContent, owner);
			if (Regex.Match(elementDeclaration, @"__PARENTHESIS__[0-9]+__").Success)
				return new MethodDefinition(elementDeclaration, elementContent, owner);
			return new PropertyDefinition(elementDeclaration, elementContent, owner);
		}

		public static CodeFile ReadCodeFile(string filePath)
		{
			if (!File.Exists(filePath))
				throw new FileNotFoundException("File not found");
			var codeFile = new CodeFile();
			codeFile.Name = filePath;
			var contents = File.ReadAllText(filePath);
			codeFile.Content = contents;
			var newContent = contents;
			if (codeFile.HasPreprocessing())
			{
				codeFile.OmittedFromAnalysis = true;
				return codeFile;
			}
			newContent = codeFile.HandlePreProcessing(newContent);
			newContent = codeFile.ClearDeclaredStrings(new List<CodeString>(), newContent);
			newContent = RemoveComments(newContent);
			newContent = codeFile.GetCodeBlocks(newContent);
			newContent = codeFile.GetNamespaceDefinitions(newContent);
			newContent = codeFile.GetGlobalTypes(newContent);
			return codeFile;
		}

		public static CodeFile CreateCodeFileFromContents(string fileContents, string fileVersion, string filePath)
		{
			var codeFile = new CodeFile();
			codeFile.Name = fileVersion + ": " + filePath;
			codeFile.Content = fileContents;
			var newContent = fileContents;
			if (codeFile.HasPreprocessing())
			{
				codeFile.OmittedFromAnalysis = true;
				return codeFile;
			}
			newContent = codeFile.HandlePreProcessing(newContent);
			newContent = codeFile.ClearDeclaredStrings(new List<CodeString>(), newContent);
			newContent = RemoveComments(newContent);
			newContent = codeFile.GetCodeBlocks(newContent);
			newContent = codeFile.GetNamespaceDefinitions(newContent);
			newContent = codeFile.GetGlobalTypes(newContent);
			return codeFile;
		}

		public static string DescribeDifferencesFromContents(string sourceContents, string targetContents, string filePath)
		{
			try
			{
				var sourceFile = CreateCodeFileFromContents(sourceContents, "source", filePath);
				var targetFile = CreateCodeFileFromContents(targetContents, "target", filePath);
				return new CodeFileComparer(sourceFile, targetFile, null).DescribeDifferences();
			}
			catch
			{
				return "Could not read file: " + filePath;
			}
		}

        public static string ExpandAllSymbols(string condensedContent, bool includeSymbolInOutput = true)
		{
			var newContent = condensedContent;
			if (string.IsNullOrWhiteSpace(condensedContent))
				return string.Empty;
			while(Regex.Match(newContent, @"(BLOCK#|STRING#|__GENERIC_TYPE__|__OPERATOR__|__PARENTHESIS__|__ATTRIBUTE_BLOCK__)[0-9]+").Success)
			{
				newContent = Operator.ExpandContent(newContent);
				newContent = GenericTypeBlock.ExpandContent(newContent, includeSymbolInOutput);
				newContent = ParenthesisBlock.ExpandContent(newContent, includeSymbolInOutput);
				newContent = CodeBlock.ExpandContent(newContent, includeSymbols: includeSymbolInOutput);
				newContent = CodeString.ExpandContent(newContent, includeSymbolInOutput);
				newContent = AttributeBlock.ExpandContent(newContent, includeSymbolInOutput);
			}
			return newContent;
		}

		public static string AsSingleLine(this string codeString)
		{
			return Regex.Replace(codeString, @"\n|\r", " ");
		}

        public static string AsList(this List<string> list)
        {
            var stringList = new StringBuilder();
            foreach (var item in list)
            {
                stringList.AppendLine(item);
            }
            return stringList.ToString().Trim();
        }

		public static string AsCSV(this List<string> list)
		{
			var stringList = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				if (i > 0)
					stringList.Append(", ");
				stringList.Append(list[i]);
			}
			return stringList.ToString().Trim();
		}
	}
}
