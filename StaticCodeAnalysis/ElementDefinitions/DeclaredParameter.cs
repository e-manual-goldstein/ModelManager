using StaticCodeAnalysis.CodeStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.Types
{
	public class DeclaredParameter : IStaticCodeElement, IHasAttributes
	{
		public DeclaredParameter(string content, IStaticCodeElement owner)
		{
			Content = CodeUtils.ExpandAllSymbols(content, true);
			Owner = owner;
			Elements = new List<IStaticCodeElement>();
			Attributes = new List<DeclaredAttribute>();
			getParameterAttributes(ref content);
			_condensedDefaultValue = getDefaultValue(ref content);
			Name = getParameterName(content);
			ParameterType = getParameterType(content);
		}

		public string Name { get; set; }

		public string ParameterType { get; set; }

		private string _condensedDefaultValue;
		public string DefaultValue
		{
			get
			{
				return string.IsNullOrWhiteSpace(_condensedDefaultValue) ? null : CodeUtils.ExpandAllSymbols(_condensedDefaultValue); 
			}
		}

		public string Content { get; set; }

		public List<DeclaredAttribute> Attributes { get; set; }

		public IStaticCodeElement Owner { get; set; }

		public List<IStaticCodeElement> Elements { get; set; }

		public bool IsGenericType { get; set; }

		private void getParameterAttributes(ref string parameterContent)
		{
			if (!parameterContent.Contains("__ATTRIBUTE_BLOCK__"))
				return;
			AttributeBlock.GetAttributes(ref parameterContent, this);
		}

		private string getDefaultValue(ref string content)
		{
			if (!content.Contains("="))
				return "";
			var defaultParamPattern = @"=(?'DefaultValue'[\s\w#\.]+)";
			var defaultValue = Regex.Match(content, defaultParamPattern).Groups["DefaultValue"].Value;
			content = content.Replace("=" + defaultValue, "").Trim();
			return CodeString.ExpandContent(defaultValue.Trim());
		}

		private string getParameterType(string content)
		{
			var typePattern = @"^(?'Type'[\s\w\[\.\?\]]*)\s\w*";
			var type = Regex.Match(content, typePattern).Groups["Type"].Value;
			if (string.IsNullOrWhiteSpace(type))
				throw new CodeParseException("Could not parse Parameter type");
			return GenericTypeBlock.ExpandContent(type);
		}

		private string getParameterName(string content)
		{
			var namePattern = @"^[\s\w\[\]\?\.\:]*\s(?'Name'\w*)";
			var name = Regex.Match(content, namePattern).Groups["Name"].Value;
			if (string.IsNullOrWhiteSpace(name))
				throw new CodeParseException("Could not parse Parameter name");
			return name;
		}
	}
}
