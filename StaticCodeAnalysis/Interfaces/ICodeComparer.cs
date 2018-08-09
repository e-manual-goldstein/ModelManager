using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis
{
	public interface ICodeComparer
	{
		List<ICodeDifference> Differences { get; set; }

		List<ICodeDifference> GetDifferences(bool recurse = false);

        void AddDifference(ICodeDifference feature);

        ICodeComparer Parent { get; set; }
    }
}
