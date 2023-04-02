namespace AssemblyAnalyser.TestData.Basics
{
    public interface IBasicInterface
    {
        string ReadOnlyInterfaceImpl { get; }
        string ReadWriteInterfaceImpl { get; set; }
    }
}