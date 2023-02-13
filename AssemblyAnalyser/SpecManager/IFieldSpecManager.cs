using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public interface IFieldSpecManager
    {
        IReadOnlyDictionary<FieldInfo, FieldSpec> Fields { get; }

        FieldSpec[] TryLoadFieldSpecs(Func<FieldInfo[]> value, TypeSpec declaringType);

        void ProcessLoadedFields(bool includeSystem = true);
    }
}
