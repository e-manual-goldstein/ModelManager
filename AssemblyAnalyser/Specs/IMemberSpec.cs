namespace AssemblyAnalyser
{
    public interface IMemberSpec : ISpec
    {
        TypeSpec DeclaringType { get; }
        TypeSpec ResultType { get; }
    }
}