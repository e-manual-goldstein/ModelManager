namespace AssemblyAnalyser
{
    public interface IHandleFaults
    {
        void AddFault(string faultMessage);
        void AddFault(FaultSeverity severity, string faultMessage);
        string[] Faults { get; }
    }
}