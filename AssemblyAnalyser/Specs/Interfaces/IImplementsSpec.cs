namespace AssemblyAnalyser
{
    internal interface IImplementsSpec<TSpec> where TSpec : AbstractSpec
    {
        TSpec[] ImplementationFor { get; }

        void RegisterAsImplementation(TSpec implementedSpec);
    }
}