using AssemblyAnalyser.Specs;

namespace AssemblyAnalyser
{
    public class MethodToTypeDependency : AbstractDependency<TypeSpec, MethodSpec>
    {
        public MethodToTypeDependency(TypeSpec parent, MethodSpec child) : base(parent, child)
        {

        }
    }
}
