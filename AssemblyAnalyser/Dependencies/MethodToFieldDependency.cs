using AssemblyAnalyser.Specs;

namespace AssemblyAnalyser
{
    public class MethodToFieldDependency : AbstractDependency<FieldSpec, MethodSpec>
    {
        public MethodToFieldDependency(FieldSpec parent, MethodSpec child) : base(parent, child)
        {

        }
    }
}