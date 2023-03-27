using AssemblyAnalyser.Specs;

namespace AssemblyAnalyser
{
    public class TypeToModuleDependency : AbstractDependency<TypeSpec, ModuleSpec>
    {
        public TypeToModuleDependency(TypeSpec requiredBy, ModuleSpec dependsOn) 
            : base(requiredBy, dependsOn) 
        {

        }


    }
}
