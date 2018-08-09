using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis
{
	public interface IHasParameters
	{
		List<DeclaredParameter> Parameters { get; set; }
	}
}
