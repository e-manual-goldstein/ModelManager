using StaticCodeAnalysis.CodeStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.Types
{
	public abstract class AbstractElementDefinition : IStaticCodeElement, IHasModifiers, IHasAttributes
	{
		public IStaticCodeElement Owner { get; set; }

		public List<IStaticCodeElement> Elements { get; set; }

		public virtual string Name { get; set; }

		public string Content { get; set; }

		protected string _condensedContent;

		private List<string> _modifiers = new List<string>();
		public List<string> Modifiers
		{
			get => _modifiers;
			set => _modifiers = value;
		}

        private List<DeclaredAttribute> _attributes = new List<DeclaredAttribute>();

		public List<DeclaredAttribute> Attributes
        {
            get => _attributes;
            set => _attributes = value;
        }

		#region Member Modifiers

		#region Access Modifiers

		public bool IsPublic
		{
			get
			{
				return Modifiers.Contains("public");
			}
		}

		public bool IsProtected
		{
			get
			{
				return Modifiers.Contains("protected");
			}
		}

		public bool IsInternal
		{
			get
			{
				return Modifiers.Contains("internal");
			}
		}

		public bool IsPrivate
		{
			get
			{
				return Modifiers.Contains("private") || (!IsPublic && !IsProtected && !IsInternal);
			}
		}


		#endregion

		#region Other Modifiers

		public bool IsStatic
		{
			get
			{
				return Modifiers.Contains("static");
			}
		}

		public bool IsAbstract
		{
			get
			{
				return Modifiers.Contains("abstract");
			}
		}

		#endregion

		#endregion

		public int LineCount { get; set; }

		protected virtual int GetLineCount(string elementContent)
		{
			var newContent = elementContent;
			newContent = CodeUtils.ExpandAllSymbols(newContent);
			newContent = IterationBlock.SymboliseBlock(newContent, this);
			return Regex.Matches(newContent, @";").Count;
		}

	}
}
