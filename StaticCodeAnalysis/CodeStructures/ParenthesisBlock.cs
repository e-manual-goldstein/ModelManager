using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeStructures
{
	public class ParenthesisBlock : ICodeSymbol
	{
		private static IDictionary<int, ParenthesisBlock> _parenthesisBlocks = new Dictionary<int, ParenthesisBlock>();
		public static IDictionary<int, ParenthesisBlock> ParenthesisBlocks
		{
			get => _parenthesisBlocks;
			set => _parenthesisBlocks = value;
		}

		private static int _parenthesisBlockCount = 0;
		public static int ParenthesisBlockCount { get => _parenthesisBlockCount; set => _parenthesisBlockCount = value; }

		public ParenthesisBlock(string parenthesisContent, IStaticCodeElement owner = null)
		{
			ParenthesisBlockId = ParenthesisBlockCount++;
			ParenthesisBlocks[ParenthesisBlockId] = this;
			ParenthesisContent = parenthesisContent.Replace("(","").Replace(")","");
			Owner = owner;
		}

		public string ParenthesisContent { get; set; }

		public int ParenthesisBlockId { get; set; }

		public IStaticCodeElement Owner { get; set; }

		public override string ToString()
		{
			return ParenthesisContent;
		}

        public static string ExpandContent(string content, bool includeSymbols = false)
        {
            var parenthesisPattern = @"__PARENTHESIS__(?'BlockId'[0-9]+)__";
            var match = Regex.Match(content, parenthesisPattern);
            while (match.Success)
            {
                content = ExpandParenthesisBlock(content, match.Groups["BlockId"].Value, includeSymbols);
                match = Regex.Match(content, parenthesisPattern);
            }
            return content;
        }

        public static string ExpandParenthesisBlock(string content, string blockId, bool includeSymbols = false)
		{
			var innerBlock = InnerBlock(content, true);
			var replacementText = includeSymbols ? "(" + innerBlock + ")" : innerBlock;
			content = content.Replace("__PARENTHESIS__" + blockId + "__", replacementText);
			return content;
		}

		public static string InnerBlock(string content, bool hasMultipleMatches = false)
		{
			var parenthesisPattern = @"__PARENTHESIS__(?'BlockId'[0-9]+)__";
			if (Regex.Matches(content, parenthesisPattern).Count > 1 && !hasMultipleMatches)
				throw new CodeParseException("More than one match found");
			var blockId = Regex.Match(content, parenthesisPattern).Groups["BlockId"].Value;
			if (string.IsNullOrWhiteSpace(blockId))
				return "";
			int id = 0;
			if (!int.TryParse(blockId, out id))
				throw new ArgumentException("Could not parse Block ID from input: " + blockId);
			if (!ParenthesisBlocks.ContainsKey(id))
				throw new ArgumentException("No matching Parenthesis Block found for ID: " + blockId);
			return ParenthesisBlocks[id].ParenthesisContent;
		}

		public static string SymboliseBlock(string content, IStaticCodeElement owner)
		{
			var parenthesisPattern = @"(?'Parenthesis'\([^\(\)]*\))";
			while (Regex.Match(content, @"[\(\)]").Success)
			{
				var parenthesisBlockMatches = Regex.Matches(content, parenthesisPattern);
				foreach (Match match in parenthesisBlockMatches)
				{
					var parenthesisBlockContent = match.Groups["Parenthesis"].Value;
					var parenthesisBlock = new ParenthesisBlock(match.Value, owner);
					content = content.Replace(parenthesisBlockContent, @"__PARENTHESIS__" + parenthesisBlock.ParenthesisBlockId + "__");
				}
			}
			if (Regex.Match(content, @"\(|\)").Success)
				throw new CodeParseException("Failed to symbolise content");
			return content;
		}
	}
}
