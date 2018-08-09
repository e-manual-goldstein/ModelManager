using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeStructures
{
	public class IterationBlock : ICodeSymbol
	{
		private static IDictionary<int, IterationBlock> _iterationBlock = new Dictionary<int, IterationBlock>();
		public static IDictionary<int, IterationBlock> IterationBlocks
		{
			get => _iterationBlock;
			set => _iterationBlock = value;
		}

		private static int _iterationBlockCount = 0;
		public static int IterationBlockCount { get => _iterationBlockCount; set => _iterationBlockCount = value; }

		public IterationBlock(string iterationBlockContent, IStaticCodeElement owner = null)
		{
			IterationBlockId = IterationBlockCount++;
			IterationBlocks[IterationBlockId] = this;
			IterationBlockContent = iterationBlockContent;
			Owner = owner;
		}

		public string IterationBlockContent { get; set; }

		public int IterationBlockId { get; set; }

		public IStaticCodeElement Owner { get; set; }

		public override string ToString()
		{
			return IterationBlockContent;
		}

		public static string SymboliseBlock(string content, IStaticCodeElement owner)
		{
			var newContent = content.AsSingleLine();
			var iterationBlockPattern = @"(^|\s*)(?'IterationBlock'(for|while|foreach)\s*\([^\n]*?\))";
			while (Regex.Match(newContent, iterationBlockPattern).Success)
			{
				var matches = Regex.Matches(newContent, iterationBlockPattern);
				foreach (Match match in matches)
				{
					var iterationBlockDefinition = match.Groups["IterationBlock"].Value;
					var iterationBlock = new IterationBlock(iterationBlockDefinition, owner);
					newContent = newContent.Replace(iterationBlockDefinition, @"__ITERATION_BLOCK__" + iterationBlock.IterationBlockId + "__;");
				}
			}
			return newContent;
		}

		public static string ExpandContent(string content)
		{
			var iterationBlockPattern = @"__ITERATION_BLOCK__(?'BlockId'[0-9]+)__;";
			var match = Regex.Match(content, iterationBlockPattern);
			while (match.Success)
			{
				content = ExpandIterationBlock(content, match.Groups["BlockId"].Value);
				match = Regex.Match(content, iterationBlockPattern);
			}
			return content;
		}

		public static string ExpandIterationBlock(string content, string blockId)
		{
			int id = 0;
			if (!int.TryParse(blockId, out id))
				throw new ArgumentException("Could not parse Block ID from input: " + blockId);
			if (!IterationBlocks.ContainsKey(id))
				throw new ArgumentException("No matching Iteration Block found for ID: " + blockId);
			content = content.Replace("__ITERATION_BLOCK__" + blockId + "__;", IterationBlocks[id].IterationBlockContent);
			return content;
		}

		public static string InnerBlock(string content)
		{
			var iterationBlockPattern = @"__ITERATION_BLOCK__(?'BlockId'[0-9]+)__;";
			if (Regex.Matches(content, iterationBlockPattern).Count > 1)
				throw new CodeParseException("More than one match found");
			var blockId = Regex.Match(content, iterationBlockPattern).Groups["BlockId"].Value;
			int id = 0;
			if (!int.TryParse(blockId, out id))
				throw new ArgumentException("Could not parse Block ID from input: " + blockId);
			if (!IterationBlocks.ContainsKey(id))
				throw new ArgumentException("No matching Iteration Block found for ID: " + blockId);
			return IterationBlocks[id].IterationBlockContent;
		}
	}
}
