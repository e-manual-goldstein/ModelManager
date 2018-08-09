using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
    public abstract class MemberComparer<TMember> : ElementComparer<TMember> where TMember : MemberDefinition
    {
        public MemberComparer(TMember sourceMember, TMember targetMember, ICodeComparer parent) : base(sourceMember, targetMember, parent)
        {
        }

        public override void Compare()
        {
			base.Compare();
            if (SourceElementDefinition.ReturnType != TargetElementDefinition.ReturnType)
            {
                CreateNewDifference("ReturnType");
            }
            if (SourceElementDefinition.IsVirtual != TargetElementDefinition.IsVirtual)
            {
                CreateNewDifference("IsVirtual");
            }
            if (SourceElementDefinition.IsOverride != TargetElementDefinition.IsOverride)
            {
                CreateNewDifference("IsOverride");
            }
            if (SourceElementDefinition.IsDelegate != TargetElementDefinition.IsDelegate)
            {
                CreateNewDifference("IsDelegate");
            }
            if (SourceElementDefinition.IsEvent != TargetElementDefinition.IsEvent)
            {
                CreateNewDifference("IsEvent");
            }
        }
    }
}
