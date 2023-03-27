using AssemblyAnalyser.Specs;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface ISpecManager : IModuleSpecManager, ITypeSpecManager, IMethodSpecManager, IParameterSpecManager, 
        IPropertySpecManager, IFieldSpecManager, IAttributeSpecManager, IEventSpecManager
    {
        void SetWorkingDirectory(string workingDirectory);
        void ProcessSpecs<TSpec>(IEnumerable<TSpec> specs, bool parallelProcessing = true) where TSpec : AbstractSpec;
        void Reset();
        void ProcessAll(bool includeSystem = true, bool parallelProcessing = true);
        ISpecDependency RegisterOperandDependency(object operand, MethodSpec methodSpec);
        //void RegisterDependency(ISpec dependingSpec, ModuleDefinition targetSpec);
    }
}