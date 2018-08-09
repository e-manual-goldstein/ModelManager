using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
	public class NamespaceComparer : AbstractComparer<NamespaceDefinition>
	{
		public NamespaceComparer(NamespaceDefinition sourceNamespace, NamespaceDefinition targetNamespace, ICodeComparer parent) :
			base(sourceNamespace, targetNamespace, parent)
		{

		}

		public override void Compare()
		{
            CompareElements(SourceElementDefinition.DefinedTypes, TargetElementDefinition.DefinedTypes, "DefinedTypes");
		}

	}
}
