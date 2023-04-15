namespace AssemblyAnalyser
{
    public interface IHasExplicitName : IHasName
    {
        string ExplicitName { get; }
    }
}