using AssemblyAnalyser.Specs;

namespace AssemblyAnalyser
{
    public class TypeToModuleDependency : AbstractDependency<ModuleSpec, TypeSpec>
    {
        public TypeToModuleDependency(ModuleSpec parent, TypeSpec child)
            : base(parent, child)
        {

        }
    }
}
