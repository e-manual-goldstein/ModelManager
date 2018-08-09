using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.Types
{
	public class CodeCompareException : Exception
	{
		public CodeCompareException(string message) : base(message)
		{

		}

		public CodeCompareException(string message, Exception innerException) : base(message, innerException)
		{

		}
	}
}
