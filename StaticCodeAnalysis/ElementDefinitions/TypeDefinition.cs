using StaticCodeAnalysis.CodeStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StaticCodeAnalysis.Types
{
	public abstract class TypeDefinition : AbstractElementDefinition, IHasTypeModifiers
	{
		public TypeDefinition(string typeDeclaration, string typeContents, IStaticCodeElement owner)
		{
			Modifiers = parseTypeModifiers(ref typeDeclaration);
			Name = getTypeNameFromDeclaration(typeDeclaration);
			Content = CodeUtils.ExpandAllSymbols(typeContents);
			Owner = owner;
			Elements = new List<IStaticCodeElement>();
			owner.Elements.Add(this);
			LineCount = GetLineCount(typeContents);
		}

		public abstract string TypeKey { get; }

        public string Namespace
        {
            get
            {
                IStaticCodeElement element = this;
                while (!(Owner is NamespaceDefinition) && Owner != null)
                {
                    element = element.Owner;
                }
                var ns = element.Owner as NamespaceDefinition;
                return ns.Name;
            }
        }

		#region Type Members

		public List<MethodDefinition> Methods
		{
			get
			{
				return Elements.OfType<MethodDefinition>().ToList();
			}
		}

		public List<MethodDefinition> Constructors
		{
			get
			{
				return Methods.Where(m => m.MethodType == DefinedMethodType.Constructor).ToList();
			}
		}

		public List<PropertyDefinition> Properties
		{
			get
			{
				return Elements.OfType<PropertyDefinition>().ToList();
			}
		}

		public List<FieldDefinition> Fields
		{
			get
			{
				return Elements.OfType<FieldDefinition>().ToList();
			}
		}

		public List<TypeDefinition> NestedTypes
		{
			get
			{
				return Elements.OfType<TypeDefinition>().ToList();
			}
		}


        private List<string> _extensions = new List<string>();
		public List<string> Extensions
        {
            get => _extensions;
            set => _extensions = value;
        }

		#endregion

		private string getTypeNameFromDeclaration(string typeDeclaration)
		{
			var pattern = @"^([\w\s])+\s(?'TypeName'(\w+))$";
			var name = Regex.Match(typeDeclaration, pattern).Groups["TypeName"].Value;
			if (string.IsNullOrEmpty(typeDeclaration))
				throw new CodeParseException("Could not parse Type name from declartion");
			return name;
		}

		private List<string> parseTypeModifiers(ref string typeDeclaration)
		{
			var modifiers = new List<string>();
			foreach (var modifier in CodeUtils.ModifierLookup().Keys)
			{
				var modifierPattern = @"(^|\s)" + modifier + @"\s";
				if (Regex.Match(typeDeclaration, modifierPattern).Success)
				{
					modifiers.Add(modifier);
					typeDeclaration = typeDeclaration.Replace(modifier, "");
				}
			}
			return modifiers;
		}

		public override string ToString()
        {
            return "Type Definition: " + Name;
        }
	}
}