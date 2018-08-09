using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
    public class AttributeComparer : AbstractComparer<DeclaredAttribute>
    {
        public AttributeComparer(DeclaredAttribute sourceAttribute, DeclaredAttribute targetAttribute, ICodeComparer parent) :
            base(sourceAttribute, targetAttribute, parent)
        {

        }

        public override void Compare()
        {
            CompareLists(SourceElementDefinition.Parameters, TargetElementDefinition.Parameters, "Parameters");
            if (SourceElementDefinition.Content != TargetElementDefinition.Content)
            {
                CreateNewDifference("Content");
            }
        }
    }
}
