using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
    public class ParameterComparer : AbstractComparer<DeclaredParameter>
    {
        public ParameterComparer(DeclaredParameter sourceParameter, DeclaredParameter targetParameter, ICodeComparer parent) : 
            base(sourceParameter, targetParameter, parent)
        {

        }

        public override void Compare()
        {
            compareAttributes();
            if (SourceElementDefinition.DefaultValue != TargetElementDefinition.DefaultValue)
            {
                CreateNewDifference("DefaultValue");
            }
            if (SourceElementDefinition.ParameterType != TargetElementDefinition.ParameterType)
            {
                CreateNewDifference("ParameterType");
            }
            if (SourceElementDefinition.IsGenericType != TargetElementDefinition.IsGenericType)
            {
                CreateNewDifference("IsGenericType");
            }
            if (SourceElementDefinition.Content != TargetElementDefinition.Content)
            {
                CreateNewDifference("Content");
            }
        }

        private void compareAttributes()
        {
            CompareElements(SourceElementDefinition.Attributes, TargetElementDefinition.Attributes, "Attributes");
        }
    }
}
