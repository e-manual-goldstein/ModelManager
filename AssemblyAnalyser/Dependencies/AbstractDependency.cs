namespace AssemblyAnalyser.Specs
{
    public abstract class AbstractDependency<TRequiredBy, TDependsOn> : ISpecDependency
        where TRequiredBy : AbstractSpec
        where TDependsOn : AbstractSpec
    {
        public AbstractDependency(TRequiredBy requiredBy, TDependsOn dependsOn)
        { 
            RequiredBy = requiredBy;
            DependsOn = dependsOn;

            requiredBy.RegisterAsRequiredBy(this);
            dependsOn.RegisterDependency(this);
        }

        public TRequiredBy RequiredBy { get; set; }

        public TDependsOn DependsOn { get; set; }

        public override string ToString()
        {
            return $"{RequiredBy} --> {DependsOn}";
        }
    }
}
