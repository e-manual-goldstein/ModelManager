using StaticCodeAnalysis.Types;
using System.Collections.Generic;

namespace StaticCodeAnalysis
{
    public interface IHasAttributes
    {
        List<DeclaredAttribute> Attributes { get; set; }
    }
}