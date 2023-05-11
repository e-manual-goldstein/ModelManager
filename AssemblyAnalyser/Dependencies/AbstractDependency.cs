namespace AssemblyAnalyser.Specs
{
    public abstract class AbstractDependency<TParent, TChild> : ISpecDependency
        where TParent : AbstractSpec
        where TChild : AbstractSpec
    {
        public AbstractDependency(TParent parent, TChild child)
        { 
            Parent = parent;
            Child = child;

            if (parent == null || child == null)
            {

            }

            parent.AddChild(this);
            child.AddParent(this);
        }

        public TParent Parent { get; set; }

        public TChild Child { get; set; }

        public override string ToString()
        {
            return $"{Child.Name} depends on {Parent.Name}";
        }
    }
}
