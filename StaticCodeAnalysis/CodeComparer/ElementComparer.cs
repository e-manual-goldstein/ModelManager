using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
	public abstract class ElementComparer<TElement> : AbstractComparer<TElement> where TElement : AbstractElementDefinition
	{
		public ElementComparer(TElement sourceElement, TElement targetElement, ICodeComparer parent) : base(sourceElement, targetElement, parent)
		{
		}

		public override void Compare()
		{
			compareAttributes();
			if (SourceElementDefinition.LineCount != TargetElementDefinition.LineCount)
			{
				CreateNewDifference("LineCount");
			}
            if (SourceElementDefinition.IsAbstract != TargetElementDefinition.IsAbstract)
            {
                CreateNewDifference("IsAbstract");
            }
            if (SourceElementDefinition.IsPublic != TargetElementDefinition.IsPublic)
            {
                CreateNewDifference("IsPublic");
            }
            if (SourceElementDefinition.IsProtected != TargetElementDefinition.IsProtected)
            {
                CreateNewDifference("IsProtected");
            }
            if (SourceElementDefinition.IsInternal != TargetElementDefinition.IsInternal)
            {
                CreateNewDifference("IsInternal");
            }
            if (SourceElementDefinition.IsPrivate != TargetElementDefinition.IsPrivate)
            {
                CreateNewDifference("IsPrivate");
            }
            if (SourceElementDefinition.IsStatic != TargetElementDefinition.IsStatic)
            {
                CreateNewDifference("IsStatic");
            }
			if (SourceElementDefinition.Content != TargetElementDefinition.Content)
			{
				CreateNewDifference("Content");
			}
		}

		public void compareAttributes()
		{
			CompareElements(SourceElementDefinition.Attributes, TargetElementDefinition.Attributes, "Attributes");
		}
	}
}
