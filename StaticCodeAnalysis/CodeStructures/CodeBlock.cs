using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeStructures
{
	public class CodeBlock : ICodeSymbol
	{
		public CodeBlock(string contents, bool isEmpty = false)
		{
			CodeBlockId = BlockCount++;
			CodeBlocks[CodeBlockId] = this;
			Contents = contents;
			IsEmpty = isEmpty;
			InnerCodeBlocks = GetInnerCodeBlocks(contents);
		}

		private static IDictionary<int, CodeBlock> _codeBlocks = new Dictionary<int, CodeBlock>();
		public static IDictionary<int, CodeBlock> CodeBlocks
		{
			get => _codeBlocks;
			set => _codeBlocks = value;
		}

		private static int _blockCount = 0;
		public static int BlockCount { get => _blockCount; set => _blockCount = value; }

		public int CodeBlockId { get; set; }

		public string Contents { get; set; }

		public bool IsEmpty { get; set; }

		private string _condensedContent;

		public string CondensedContent
		{
			get => _condensedContent;
		}

		public List<CodeBlock> InnerCodeBlocks { get; set; }

		public List<CodeBlock> GetInnerCodeBlocks(string content)
		{
			var codeBlocks = new List<CodeBlock>();
			_condensedContent = CodeUtils.GetCodeBlocks(codeBlocks, content);
			return codeBlocks;
		}

		public override string ToString()
		{
			return Contents.Length > 50 ? Contents.Substring(0, 50) : Contents;
		}

		public static string ExpandContent(string content, bool recurse = false, bool includeSymbols = false)
		{
			var blockPattern = @"BLOCK#(?'BlockId'[0-9]+)#";
			var match = Regex.Match(content, blockPattern);
			var continueExpansion = match.Success;
			while (continueExpansion)
			{
				content = ExpandBlock(content, match.Groups["BlockId"].Value, includeSymbols);
				match = Regex.Match(content, blockPattern);
				continueExpansion = match.Success && recurse;
			}
			return content;
		}

		public static string ExpandBlock(string content, string blockId, bool includeSymbols = false)
		{
			int id = 0;
			if (!int.TryParse(blockId, out id))
				throw new ArgumentException("Could not parse Block ID from input: " + blockId);
			if (!CodeBlocks.ContainsKey(id))
				throw new ArgumentException("No matching Code Block found for ID: " + blockId);
			var replacementText = includeSymbols ? "{" + CodeBlocks[id].Contents + "}" : CodeBlocks[id].Contents;
			content = content.Replace("BLOCK#" + blockId + "#", replacementText);
			return content;
		}
	}
}
