
using AssemblyAnalyser.Specs;

namespace AssemblyAnalyser
{
    public class MethodToMethodDependency : AbstractDependency<MethodSpec, MethodSpec>
    {
        public MethodToMethodDependency(MethodSpec requiredBy, MethodSpec dependsOn) : base(requiredBy, dependsOn)
        {
        }
    }
}
