using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public interface IAttributeSpecManager
    {
        IReadOnlyDictionary<string, TypeSpec> Attributes { get; }

        TypeSpec[] TryLoadAttributeSpecs(Func<CustomAttributeData[]> value, AbstractSpec decoratedSpec);
        TypeSpec[] TryLoadAttributeSpecs(Func<CustomAttribute[]> value, AbstractSpec decoratedSpec);

        void ProcessLoadedAttributes(bool includeSystem = true);
    }
}
