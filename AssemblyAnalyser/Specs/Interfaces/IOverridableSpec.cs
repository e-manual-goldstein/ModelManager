namespace AssemblyAnalyser
{
    internal interface IOverridableSpec<TMemberSpec> where TMemberSpec : IMemberSpec
    {
        TMemberSpec BaseSpec { get; }
    }
}