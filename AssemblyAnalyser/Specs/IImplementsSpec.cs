namespace AssemblyAnalyser
{
    internal interface IImplementsSpec<TSpec> where TSpec : AbstractSpec
    {
        TSpec Implements { get; }

        void RegisterAsImplementation(TSpec implementedSpec);
    }
}