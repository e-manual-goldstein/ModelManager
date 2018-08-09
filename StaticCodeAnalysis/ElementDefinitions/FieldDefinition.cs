using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using StaticCodeAnalysis.CodeStructures;

namespace StaticCodeAnalysis.Types
{
	public class FieldDefinition : MemberDefinition
	{
		public FieldDefinition(string fieldDeclaration, string fieldContent, IStaticCodeElement owner) : base(fieldDeclaration, fieldContent, owner)
		{

		}

		private FieldDefinition(string name, string returnType, string content, List<string> modifiers, IStaticCodeElement owner) : 
			base(name, returnType, content, modifiers, owner)
		{
			owner.Elements.Add(this);
		}

		protected override string getMemberNameFromDeclaration(string fieldDeclaration)
        {
            var pattern = @"^(?'ReturnType'[\:\.\w\s\?]+)\s(?'MemberName'[\s\,\w]+)$";
			var newDeclaration = GenericTypeBlock.SymboliseBlock(fieldDeclaration, this);
            var name = Regex.Match(newDeclaration, pattern).Groups["MemberName"].Value;
			if (string.IsNullOrWhiteSpace(name))
				throw new CodeParseException("Could not parse Field name");
			ReturnType = CodeUtils.ExpandAllSymbols(Regex.Match(newDeclaration, pattern).Groups["ReturnType"].Value, true);
			if (string.IsNullOrWhiteSpace(ReturnType))
				throw new CodeParseException("Could not parse Field return type");
			var fieldArray = name.Split(',');
			for (int i = 1; i < fieldArray.Count(); i++)
			{
				new FieldDefinition(fieldArray[i].Trim(), ReturnType, Content, Modifiers, Owner);
			}
			
            return fieldArray[0];
        }

		public bool IsReadOnly
		{
			get
			{
				return Modifiers.Contains("readonly");
			}
		}

		public bool IsConst
		{
			get
			{
				return Modifiers.Contains("const");
			}
		}
	}
}
