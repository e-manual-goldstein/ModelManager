using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace StaticCodeAnalysis.CodeStructures
{
	public class Operator : ICodeSymbol
	{
		private static IDictionary<int, Operator> _operators = new Dictionary<int, Operator>();
		public static IDictionary<int, Operator> Operators
		{
			get => _operators;
			set => _operators = value;
		}

		private static int _operatorCount = 0;
		public static int OperatorCount { get => _operatorCount; set => _operatorCount = value; }

		public Operator(string operatorContent, IStaticCodeElement owner = null)
		{
			OperatorId = OperatorCount++;
			Operators[OperatorId] = this;
			OperatorContent = operatorContent;
			Owner = owner;
		}

		public string OperatorContent { get; set; }

		public int OperatorId { get; set; }

		public IStaticCodeElement Owner { get; set; }

		public override string ToString()
		{
			return OperatorContent;
		}

		public static string SymboliseBlock(string content, IStaticCodeElement owner)
		{
			var operatorPattern = @"(?'Operator'\=\=|\!\=|\<\=|\>\=|\<\<|\>\>|\+\+|\-\-|\&\&|\|\||\=\>)";
			var newContent = content;
			while (Regex.Match(newContent, operatorPattern).Success)
			{
				var matches = Regex.Matches(newContent, operatorPattern);
				foreach (Match match in matches)
				{
					var operatorDefinition = match.Groups["Operator"].Value;
					var newOperator = new Operator(operatorDefinition, owner);
					newContent = newContent.Replace(operatorDefinition, @"__OPERATOR__" + newOperator.OperatorId + "__");
				}
			}
			return newContent;
		}

		public static string ExpandContent(string content)
		{
			var operatorPattern = @"__OPERATOR__(?'BlockId'[0-9]+)__";
			var match = Regex.Match(content, operatorPattern);
			while (match.Success)
			{
				content = ExpandOperator(content, match.Groups["BlockId"].Value);
				match = Regex.Match(content, operatorPattern);
			}
			return content;
		}

		public static string ExpandOperator(string content, string blockId)
		{
			int id = 0;
			if (!int.TryParse(blockId, out id))
				throw new ArgumentException("Could not parse Block ID from input: " + blockId);
			if (!Operators.ContainsKey(id))
				throw new ArgumentException("No matching Operator found for ID: " + blockId);
			content = content.Replace("__OPERATOR__" + blockId + "__", Operators[id].OperatorContent);
			return content;
		}

		public static string InnerBlock(string content)
		{
			var operatorPattern = @"__OPERATOR__(?'BlockId'[0-9]+)__";
			if (Regex.Matches(content, operatorPattern).Count > 1)
				throw new CodeParseException("More than one match found");
			var blockId = Regex.Match(content, operatorPattern).Groups["BlockId"].Value;
			int id = 0;
			if (!int.TryParse(blockId, out id))
				throw new ArgumentException("Could not parse Block ID from input: " + blockId);
			if (!Operators.ContainsKey(id))
				throw new ArgumentException("No matching Operator found for ID: " + blockId);
			return Operators[id].OperatorContent;
		}
	}

}

