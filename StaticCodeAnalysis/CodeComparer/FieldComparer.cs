using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
	public class FieldComparer : MemberComparer<FieldDefinition>
	{
		public FieldComparer(FieldDefinition sourceFieldDefinition, FieldDefinition targetFieldDefinition, ICodeComparer parent) : 
			base(sourceFieldDefinition, targetFieldDefinition, parent)
		{
		
		}

		public override void Compare()
		{
			base.Compare();
			if (SourceElementDefinition.IsConst != TargetElementDefinition.IsConst)
			{
				CreateNewDifference("IsConst");
			}
			if (SourceElementDefinition.IsReadOnly != TargetElementDefinition.IsReadOnly)
			{
				CreateNewDifference("IsReadOnly");
			}
		}
	}
}
