using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis
{
	public interface ICodeDifference
	{
		string Description();

        List<string> SourceList { get; set; }

        List<string> TargetList { get; set; }

        string Feature { get; }

        string SourceValue { get; }

        string TargetValue { get; }

        string ElementName { get; }
    }
}
