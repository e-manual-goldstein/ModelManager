using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeStructures
{
	public class GenericTypeBlock : ICodeSymbol
	{
		private static IDictionary<int, GenericTypeBlock> _genericTypeBlocks = new Dictionary<int, GenericTypeBlock>();
		public static IDictionary<int, GenericTypeBlock> GenericTypeBlocks
		{
			get => _genericTypeBlocks;
			set => _genericTypeBlocks = value;
		}

		private static int _genericTypeBlockCount = 0;
		public static int GenericTypeBlockCount { get => _genericTypeBlockCount; set => _genericTypeBlockCount = value; }

		public GenericTypeBlock(string genericTypeBlockContent, IStaticCodeElement owner = null)
		{
			GenericTypeBlockId = GenericTypeBlockCount++;
			GenericTypeBlocks[GenericTypeBlockId] = this;
			GenericTypeBlockContent = genericTypeBlockContent;
			Owner = owner;
		}

		public string GenericTypeBlockContent { get; set; }

		public int GenericTypeBlockId { get; set; }

		public IStaticCodeElement Owner { get; set; }

		public override string ToString()
		{
			return GenericTypeBlockContent;
		}

		public static string SymboliseBlock(string content, IStaticCodeElement owner)
		{
			var genericTypePattern = @"\s*<(?'GType'[,\w\.\s\?]+?)>\s*";
			var newContent = Operator.ExpandContent(content);
			while (Regex.Match(newContent, genericTypePattern).Success)
			{
				var matches = Regex.Matches(newContent, genericTypePattern);
				foreach (Match match in matches)
				{
					var genericTypeDefinition = match.Groups["GType"].Value;
					var genericTypeBlock = new GenericTypeBlock(genericTypeDefinition, owner);
					newContent = newContent.Replace("<" + genericTypeDefinition + ">", @"__GENERIC_TYPE__" + genericTypeBlock.GenericTypeBlockId + "__");
				}
			}
			if (Regex.Match(newContent, "<|>").Success)
				throw new CodeParseException("Failed to symbolise content");
			return newContent;
		}

		public static string ExpandContent(string content, bool includeAngleBracketsInOutput = true)
		{
			var genericTypePattern = @"__GENERIC_TYPE__(?'BlockId'[0-9]+)__";
			var newContent = content;
			var match = Regex.Match(newContent, genericTypePattern);
			while (match.Success)
			{
				newContent = ExpandGenericTypeBlock(newContent, match.Groups["BlockId"].Value, includeAngleBracketsInOutput);
				match = Regex.Match(newContent, genericTypePattern);
			}
			return newContent;
		}

		public static string ExpandGenericTypeBlock(string content, string blockId, bool includeAngleBracketsInOutput = false)
		{
			int id = 0;
			if (!int.TryParse(blockId, out id))
				throw new ArgumentException("Could not parse Block ID from input: " + blockId);
			if (!GenericTypeBlocks.ContainsKey(id))
				throw new ArgumentException("No matching Generic Type Block found for ID: " + blockId);
			var replacementText = includeAngleBracketsInOutput ? "<" + GenericTypeBlocks[id].GenericTypeBlockContent + ">" : GenericTypeBlocks[id].GenericTypeBlockContent;
			content = content.Replace("__GENERIC_TYPE__" + blockId + "__", replacementText);
			return content;
		}

		public static string InnerBlock(string content)
		{
			var genericTypePattern = @"__GENERIC_TYPE__(?'BlockId'[0-9]+)__";
			if (Regex.Matches(content, genericTypePattern).Count > 1)
				throw new CodeParseException("More than one match found");
            var blockId = Regex.Match(content, genericTypePattern).Groups["BlockId"].Value;
			int id = 0;
			if (!int.TryParse(blockId, out id))
				throw new ArgumentException("Could not parse Block ID from input: " + blockId);
			if (!GenericTypeBlocks.ContainsKey(id))
				throw new ArgumentException("No matching Generic Type Block found for ID: " + blockId);
			return GenericTypeBlocks[id].GenericTypeBlockContent;
		}
	}
}
