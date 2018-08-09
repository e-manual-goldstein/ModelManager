using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.Types
{
	public class DeclaredAttribute : IStaticCodeElement
	{
		public DeclaredAttribute(string attributeName, string attributeContent = "", IStaticCodeElement owner = null)
		{
			if (string.IsNullOrWhiteSpace(attributeName))
				throw new ArgumentNullException("Attribute Name cannot be null");
			Name = attributeName;
			Parameters = new List<string>();
			Owner = owner;
			Elements = new List<IStaticCodeElement>();
			getAttributeParametersFromContent(attributeContent);
		}

		private void getAttributeParametersFromContent(string attributeContent)
		{
			foreach (var parameter in attributeContent.Split(','))
			{
				var fullParameterContent = CodeUtils.ExpandAllSymbols(parameter);
				Parameters.Add(fullParameterContent);
			}
		}

		public string Name { get; set; }

		public string Content { get; set; }

		public List<string> Parameters { get; set; }

		public IStaticCodeElement Owner { get; set; }

		public List<IStaticCodeElement> Elements { get; set; }

		public override string ToString()
		{
			return "Declared Attribute: " + Name;
		}
	}
}
