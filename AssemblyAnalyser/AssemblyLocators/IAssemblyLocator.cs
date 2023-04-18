namespace AssemblyAnalyser
{
    public interface IAssemblyLocator
    {
        string LocateAssemblyByName(string assemblyName);
    }
}