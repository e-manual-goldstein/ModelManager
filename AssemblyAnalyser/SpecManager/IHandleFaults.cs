using AssemblyAnalyser.Faults;

namespace AssemblyAnalyser
{
    public interface IHandleFaults
    {
        void AddFault(string faultMessage);
        void AddFault(FaultSeverity severity, string faultMessage);
        void AddFault(ISpec context, FaultSeverity severity, string faultMessage);
        BuildFault[] Faults { get; }
        void AddMessage(string msg);
        string[] Messages { get; }
    }
}