using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.Types
{
    public class StructDefinition : TypeDefinition, IComplexType
    {
        public StructDefinition(string typeDeclaration, string structContents, string extensions, string attributes, IStaticCodeElement owner) :
			base(typeDeclaration, structContents, owner)
        {
			this.GetAttributes(attributes);
			this.GetExtensions(extensions);
		}

        public override string TypeKey
        {
            get { return "struct"; }
        }
	}
}
