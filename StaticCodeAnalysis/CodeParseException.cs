using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.Types
{
	public class CodeParseException : Exception
	{
		public CodeParseException(string message) : base(message)
		{
			
		}

		public CodeParseException(string message, Exception innerException) : base(message, innerException)
		{

		}
	}
}
