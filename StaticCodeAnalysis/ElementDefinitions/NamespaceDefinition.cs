using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StaticCodeAnalysis.Types
{
	public class NamespaceDefinition : IStaticCodeElement, IHasDefinedTypes
	{
		public NamespaceDefinition(string name, string condensedContent, IStaticCodeElement owner)
		{
			Name = name;
			Owner = owner;
			Elements = new List<IStaticCodeElement>();
			CondensedContent = condensedContent;
			var elements = CodeUtils.GetElementDefinitions(condensedContent, this);
			DefinedTypes = elements.OfType<TypeDefinition>().ToList();
		}

		public string Name { get; set; }

		public string Content { get; set; }

		public string CondensedContent { get; set; }

		public IStaticCodeElement Owner { get; set; }

		public List<IStaticCodeElement> Elements { get; set; }

		public List<ClassDefinition> Classes
		{
			get
			{
				return DefinedTypes.OfType<ClassDefinition>().ToList();
			}
		}

		public List<TypeDefinition> DefinedTypes { get; set; }

		public List<TypeDefinition> DefinedTypesIncludingNested()
		{
			return DefinedTypes.Union(DefinedTypes.OfType<IHasDefinedTypes>().SelectMany(x => x.DefinedTypesIncludingNested())).ToList();
		}

		public override string ToString()
		{
			return "Namespace Definition: " + Name;
		}
	}
}