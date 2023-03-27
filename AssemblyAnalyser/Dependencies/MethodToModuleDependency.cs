using AssemblyAnalyser.Specs;

namespace AssemblyAnalyser
{
    public class MethodToModuleDependency : AbstractDependency<MethodSpec, ModuleSpec>
    {
        public MethodToModuleDependency(MethodSpec requiredBy, ModuleSpec dependsOn)
            : base(requiredBy, dependsOn)
        {

        }
    }
}
