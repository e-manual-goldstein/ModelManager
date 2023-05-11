using AssemblyAnalyser.Specs;

namespace AssemblyAnalyser
{
    public class MethodToPropertyDependency : AbstractDependency<PropertySpec, MethodSpec>
    {
        public MethodToPropertyDependency(PropertySpec parent, MethodSpec child) : base(parent, child)
        {

        }
    }
}
