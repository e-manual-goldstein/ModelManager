namespace AssemblyAnalyser
{
    public interface IMemberSpec
    {
        TypeSpec DeclaringType { get; }
        TypeSpec ReturnType { get; }
    }
}