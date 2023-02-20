using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public interface IEventSpecManager
    {
        EventSpec[] TryLoadEventSpecs(Func<EventInfo[]> value, TypeSpec typeSpec);
        IReadOnlyDictionary<EventInfo, EventSpec> Events { get; }
        

        void ProcessLoadedEvents(bool includeSystem = true);
    }
}
