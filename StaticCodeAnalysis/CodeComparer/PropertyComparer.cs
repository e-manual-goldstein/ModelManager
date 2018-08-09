using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
    public class PropertyComparer : MemberComparer<PropertyDefinition>
    {
        public PropertyComparer(PropertyDefinition sourceProperty, PropertyDefinition targetProperty, ICodeComparer parent) :
            base(sourceProperty, targetProperty, parent)
        {
        }

        public override void Compare()
        {
			base.Compare();
            if (SourceElementDefinition.GetterBlock != TargetElementDefinition.GetterBlock)
            {
                CreateNewDifference("GetterBlock");
            }
            if (SourceElementDefinition.SetterBlock != TargetElementDefinition.SetterBlock)
            {
                CreateNewDifference("SetterBlock");
            }
        }
    }
}
