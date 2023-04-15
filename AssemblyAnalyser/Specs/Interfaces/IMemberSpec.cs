namespace AssemblyAnalyser
{
    public interface IMemberSpec : ISpec, IHasExplicitName
    {
        TypeSpec DeclaringType { get; }
        TypeSpec ResultType { get; }
    }
}