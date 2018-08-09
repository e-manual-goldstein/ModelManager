using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
    public class MethodComparer : MemberComparer<MethodDefinition>
    {
        public MethodComparer(MethodDefinition sourceMethod, MethodDefinition targetMethod, ICodeComparer parent) :
            base(sourceMethod, targetMethod, parent)
        {
        }

        public override void Compare()
        {
			base.Compare();
            compareParameters();
            if (SourceElementDefinition.MethodType != TargetElementDefinition.MethodType)
            {
                CreateNewDifference("MethodType");
            }
            if (SourceElementDefinition.BaseMethodDefinition != TargetElementDefinition.BaseMethodDefinition)
            {
                CreateNewDifference("BaseMethodDefinition");
            }
            if (SourceElementDefinition.GenericMethodType != TargetElementDefinition.GenericMethodType)
            {
                CreateNewDifference("GenericMethodType");
            }
            if (SourceElementDefinition.GenericMethodTypeConditions != TargetElementDefinition.GenericMethodTypeConditions)
            {
                CreateNewDifference("GenericMethodTypeConditions");
            }
        }

        private void compareParameters()
        {
            CompareElements(SourceElementDefinition.Parameters, TargetElementDefinition.Parameters, "Parameters");
        }
    }
}
