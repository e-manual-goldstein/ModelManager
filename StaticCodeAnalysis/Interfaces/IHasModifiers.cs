using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis
{
    public interface IHasModifiers : IStaticCodeElement
    {
        List<string> Modifiers { get; set; }
    }
}
