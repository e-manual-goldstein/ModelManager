using StaticCodeAnalysis.Types;
using StaticCodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeStructures
{
	public class AttributeBlock : ICodeSymbol
	{
		private static IDictionary<int, AttributeBlock> _attributeBlock = new Dictionary<int, AttributeBlock>();
		public static IDictionary<int, AttributeBlock> AttributeBlocks
		{
			get => _attributeBlock;
			set => _attributeBlock = value;
		}

		private static int _attributeBlockCount = 0;
		public static int AttributeBlockCount { get => _attributeBlockCount; set => _attributeBlockCount = value; }

		public AttributeBlock(string attributeBlockContent, IStaticCodeElement owner = null)
		{
			AttributeBlockId = AttributeBlockCount++;
			AttributeBlocks[AttributeBlockId] = this;
			AttributeBlockContent = attributeBlockContent;
			Owner = owner;
		}

		public string AttributeBlockContent { get; set; }

		public int AttributeBlockId { get; set; }

		public IStaticCodeElement Owner { get; set; }

		public override string ToString()
		{
			return AttributeBlockContent;
		}

		public static string SymboliseBlock(string content, IStaticCodeElement owner)
		{
			content = Regex.Replace(content, @"\]\s*\[", ",");
			var attributeBlockPattern = @"(^|\,)\s*\[(?'AttributeBlock'[,\w\.\s]+?)\]\s*";
			var newContent = content;
			while (Regex.Match(newContent, attributeBlockPattern).Success)
			{
				var matches = Regex.Matches(newContent, attributeBlockPattern);
				foreach (Match match in matches)
				{
					var attributeBlockDefinition = match.Groups["AttributeBlock"].Value;
					var attributeBlock = new AttributeBlock(attributeBlockDefinition, owner);
					newContent = newContent.Replace("[" + attributeBlockDefinition + "]", @"__ATTRIBUTE_BLOCK__" + attributeBlock.AttributeBlockId + "__");
				}
			}
			if (Regex.Match(newContent, "[|]").Success)
				throw new CodeParseException("Failed to symbolise content");
			return newContent;
		}

		public static string ExpandContent(string content, bool includeSymbol)
		{
			var attributeBlockPattern = @"__ATTRIBUTE_BLOCK__(?'BlockId'[0-9]+)__";
			var match = Regex.Match(content, attributeBlockPattern);
			while (match.Success)
			{
				content = ExpandAttributeBlock(content, match.Groups["BlockId"].Value, includeSymbol);
				match = Regex.Match(content, attributeBlockPattern);
			}
			return content;
		}

		public static string ExpandAttributeBlock(string content, string blockId, bool includeSymbols)
		{
			int id = 0;
			if (int.TryParse(blockId, out id))
			{
				var replacementText = includeSymbols ? "[" + AttributeBlocks[id].AttributeBlockContent + "]" : AttributeBlocks[id].AttributeBlockContent;
				content = content.Replace("__ATTRIBUTE_BLOCK__" + blockId + "__", replacementText);
			}
			return content;
		}

		public static string InnerBlock(string content)
		{
			var attributeBlockPattern = @"__ATTRIBUTE_BLOCK__(?'BlockId'[0-9]+)__";
			if (Regex.Matches(content, attributeBlockPattern).Count > 1)
				throw new CodeParseException("More than one match found");
			var blockId = Regex.Match(content, attributeBlockPattern).Groups["BlockId"].Value;
			int id = 0;
			if (!int.TryParse(blockId, out id))
				throw new ArgumentException("Could not parse Block ID from input: " + blockId);
			if (!AttributeBlocks.ContainsKey(id))
				throw new ArgumentException("No matching Attribute Block found for ID: " + blockId);
			return AttributeBlocks[id].AttributeBlockContent;
		}

		public static void GetAttributes(ref string content, IHasAttributes owner)
		{
			var attributeBlockPattern = @"__ATTRIBUTE_BLOCK__(?'BlockId'[0-9]+)__";
			var matches = Regex.Matches(content, attributeBlockPattern);
			foreach (Match attributeMatch in matches)
			{
				var attributeBlock = InnerBlock(content);
				owner.GetAttributes(attributeBlock);
				content = content.Replace(attributeMatch.Value, "").Trim();
			}
		}
	}
}
