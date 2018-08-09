using System.Collections.Generic;

namespace StaticCodeAnalysis.Types
{
    public class InterfaceDefinition : TypeDefinition, IStaticCodeElement, IComplexType
	{
        public InterfaceDefinition(string typeDeclaration, string typeContents, string extensions, string attributes, IStaticCodeElement owner) : base(typeDeclaration, typeContents, owner)
        {
			this.GetAttributes(attributes);
			this.GetExtensions(extensions);
		}

        public override string TypeKey
        {
            get { return "interface"; }
        }

	}
}