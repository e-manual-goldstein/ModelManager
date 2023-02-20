namespace AssemblyAnalyser.TestData
{
    public interface IBasicInterface
    {
        string ReadOnlyInterfaceImpl { get; }
        string ReadWriteInterfaceImpl { get; set; }
    }
}