using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace StaticCodeAnalysis.Types
{
	public class ClassDefinition : TypeDefinition, IComplexType
    {
        public ClassDefinition(string typeDeclarations, string typeContents, string extensions, string attributes, IStaticCodeElement owner) : base(typeDeclarations, typeContents, owner)
        {
            CodeUtils.GetElementDefinitions(typeContents, this);
            Members = Elements.OfType<MemberDefinition>().ToList();
			this.GetAttributes(attributes);
            this.GetExtensions(extensions);
		}

		public override string TypeKey
        {
            get { return "class";  }
        }

        public List<MemberDefinition> Members { get; set; }

		public bool IsPartialDefinition { get; set; }

		#region Class Modifiers 

		public virtual bool IsSealed
		{
			get
			{
				return Modifiers.Contains("sealed");
			}
		}

		#endregion
	}
}