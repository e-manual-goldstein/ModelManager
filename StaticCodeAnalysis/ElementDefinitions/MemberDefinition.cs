using StaticCodeAnalysis.CodeStructures;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StaticCodeAnalysis.Types
{
    public abstract class MemberDefinition : AbstractElementDefinition, IHasAttributes, IHasMemberModifiers
    {
		public MemberDefinition(string memberDeclaration, string memberContent, IStaticCodeElement owner)
		{
			var modifiers = parseMemberModifiers(ref memberDeclaration);
            var name = getMemberNameFromDeclaration(memberDeclaration.Trim());
			LineCount = GetLineCount(memberContent);
			init(name, memberContent, modifiers, owner);
		}

		protected MemberDefinition(string name, string returnType, string content, List<string> modifiers, IStaticCodeElement owner)
		{
			ReturnType = returnType;
			init(name, content, modifiers, owner);
		}

		private void init(string name, string content, List<string> modifiers, IStaticCodeElement owner)
		{
            Owner = owner;
			Name = CodeUtils.ExpandAllSymbols(name);
			Elements = new List<IStaticCodeElement>();
			Modifiers = modifiers;
			_condensedContent = content;
			Content = CodeUtils.ExpandAllSymbols(content, true);
			owner.Elements.Add(this);
		}

		private List<string> parseMemberModifiers(ref string memberDeclaration)
		{
			var modifiers = new List<string>();
			foreach (var modifier in CodeUtils.ModifierLookup().Keys)
			{
				var modifierPattern = @"(^|\s)" + modifier + @"\s";
				if (Regex.Match(memberDeclaration, modifierPattern).Success)
				{
					modifiers.Add(modifier);
					memberDeclaration = memberDeclaration.Replace(modifier, "");
				}
			}
			return modifiers;
		}

		protected abstract string getMemberNameFromDeclaration(string memberDeclaration);

		public string ReturnType { get; set; }
        
		#region Member Modifiers

		public bool IsVirtual
		{
			get
			{
				return Modifiers.Contains("virtual");
			}
		}

		public bool IsOverride
		{
			get
			{
				return Modifiers.Contains("override");
			}
		}

		public bool IsDelegate
		{
			get
			{
				return Modifiers.Contains("delegate");
			}
		}

		public bool IsEvent
		{
			get
			{
				return Modifiers.Contains("event");
			}
		}

        #endregion

        public override string ToString()
        {
            return "Member Definition: " + Name;
        }
    }
}