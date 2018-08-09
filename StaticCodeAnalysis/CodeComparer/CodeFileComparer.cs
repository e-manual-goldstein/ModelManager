using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
	public class CodeFileComparer : AbstractComparer<CodeFile>
	{
		public CodeFileComparer(CodeFile sourceCodeFile, CodeFile targetCodeFile, ICodeComparer parent) :
			base(sourceCodeFile, targetCodeFile, parent)
		{

		}

		public override void Compare()
		{
			compareNamespaces();
			compareGlobalTypes();
		}

		private void compareNamespaces()
		{
            CompareElements(SourceElementDefinition.Namespaces, TargetElementDefinition.Namespaces, "Namespaces");
		}

		public void compareGlobalTypes()
		{
			CompareElements(SourceElementDefinition.GlobalTypes, TargetElementDefinition.GlobalTypes, "GlobalTypes");
		}
	}
}
