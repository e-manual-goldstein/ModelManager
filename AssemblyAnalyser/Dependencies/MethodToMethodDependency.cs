
using AssemblyAnalyser.Specs;

namespace AssemblyAnalyser
{
    public class MethodToMethodDependency : AbstractDependency<MethodSpec, MethodSpec>
    {
        public MethodToMethodDependency(MethodSpec parent, MethodSpec child) : base(parent, child)
        {

        }
    }
}
