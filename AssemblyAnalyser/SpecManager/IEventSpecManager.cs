﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public interface IEventSpecManager
    {
        //EventSpec[] TryLoadEventSpecs(Func<EventDefinition[]> value, TypeSpec typeSpec);
        EventSpec[] EventSpecs { get; }
        

        //void ProcessLoadedEvents(bool includeSystem = true);
    }
}
