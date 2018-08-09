using System.Collections.Generic;

namespace StaticCodeAnalysis
{
    public interface IStaticCodeElement
    {
        string Name { get; set; }

		string Content { get; set; }

        //void Analyse(string content);

		IStaticCodeElement Owner { get; set; }

		List<IStaticCodeElement> Elements { get; set; }
    }
}