using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.Types
{
    public class PropertyDefinition : MemberDefinition
    {
        public PropertyDefinition(string propertyDeclaration, string propertyContent, IStaticCodeElement owner) : base(propertyDeclaration, propertyContent, owner)
        {
			var inlineContent = propertyContent.AsSingleLine();
			getGetterBlock(inlineContent);
			getSetterBlock(inlineContent);
        }

		private void getGetterBlock(string inlineContent)
		{
			var getterPattern = @"[^\s;]*get(?'GetterBlock'\s*BLOCK#[0-9]+#)";
			var getter = Regex.Match(inlineContent, getterPattern);
			if (getter.Success)
			{
				var block = getter.Groups["GetterBlock"].Value;
				if (block != ";")
					GetterBlock = CodeUtils.ExpandAllSymbols(block,true).Trim();
			}
		}

		private void getSetterBlock(string inlineContent)
		{
			var setterPattern = @"[^\s;]*set(?'SetterBlock'\s*BLOCK#[0-9]+#)";
			var setter = Regex.Match(inlineContent, setterPattern);
			if (setter.Success)
			{
				var block = setter.Groups["SetterBlock"].Value;
				if (block != ";")
					SetterBlock = CodeUtils.ExpandAllSymbols(block,true).Trim();
			}
		}

		protected override string getMemberNameFromDeclaration(string propertyDeclaration)
        {
            var pattern = @"^(?'ReturnType'[\:\.\w\s\<\>\,\?]+)\s+(?'MemberName'[\.\w]+)$";
			var propMatch = Regex.Match(propertyDeclaration, pattern);
			var name = propMatch.Groups["MemberName"].Value;
			if (string.IsNullOrWhiteSpace(name))
				throw new CodeParseException("Could not parse Property name");
			var returnType = propMatch.Groups["ReturnType"].Value;
			if (string.IsNullOrWhiteSpace(returnType))
				throw new CodeParseException("Could not parse Property return type");
			ReturnType = returnType;
			return name;
        }

		public string GetterBlock { get; set; }
		public string SetterBlock { get; set; }
	}
}
