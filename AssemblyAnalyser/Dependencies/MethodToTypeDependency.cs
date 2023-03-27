using AssemblyAnalyser.Specs;

namespace AssemblyAnalyser
{
    public class MethodToTypeDependency : AbstractDependency<MethodSpec, TypeSpec>
    {
        public MethodToTypeDependency(MethodSpec requiredBy, TypeSpec dependsOn)
            : base(requiredBy, dependsOn)
        {

        }
    }
}
