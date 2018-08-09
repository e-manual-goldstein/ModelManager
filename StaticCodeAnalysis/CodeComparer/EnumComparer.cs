using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
	public class EnumComparer : AbstractComparer<EnumDefinition>
	{
		public EnumComparer(EnumDefinition sourceElement, EnumDefinition targetElement) : 
			base(sourceElement, targetElement)
		{
		}

		public override void Compare()
		{
			throw new NotImplementedException();
		}
	}
}
