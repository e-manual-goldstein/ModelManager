using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Faults
{
    public class BuildFault 
    {
        const string NoSpecFaultContext = "No Spec Context for Fault";

        public BuildFault(ISpec spec, FaultSeverity faultSeverity, string message) : this(spec, message) 
        {
            Severity = faultSeverity;
        }

        public BuildFault(FaultSeverity severity, string message) : this(message)
        {
            Severity = severity;
        }

        public BuildFault(ISpec spec, string message) : this(message)
        {
            Spec = spec;            
        }

        public BuildFault(string message)
        {
            Message = message;
        }

        public ISpec Spec { get; }
        public FaultSeverity Severity { get; } = FaultSeverity.Information;
        public string Message { get; }

        public override string ToString()
        {
            return $"[{Severity}] {Message} - {Spec?.ToString() ?? NoSpecFaultContext}";
        }
    }
}
