using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.Types
{
    public class EnumDefinition : TypeDefinition, IHasAttributes
    {
        public EnumDefinition(string enumDeclaration, string enumContents, string attributeList, IStaticCodeElement owner) : base(enumDeclaration, enumContents, owner)
        {
            this.GetAttributes(attributeList);
        }

        public override string TypeKey
        {
            get { return "enum"; }
        }

        
    }
}
