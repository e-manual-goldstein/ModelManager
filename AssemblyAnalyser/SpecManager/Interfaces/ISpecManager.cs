using AssemblyAnalyser.Specs;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface ISpecManager : IAssemblySpecManager, IModuleSpecManager, ITypeSpecManager, IMethodSpecManager,
        IPropertySpecManager, IFieldSpecManager, IAttributeSpecManager, IEventSpecManager, IHandleFaults
    {
        void SetWorkingDirectory(string workingDirectory);
        void ProcessSpecs<TSpec>(IEnumerable<TSpec> specs, bool parallelProcessing = false) where TSpec : AbstractSpec;
        void Reset();
        void ProcessAll(bool includeSystem = true, bool parallelProcessing = true);
        
        ISpecDependency RegisterOperandDependency(object operand, MethodSpec methodSpec);

        TypeSpec GetNullTypeSpec();

        IRule[] SpecRules { get; }
    }
}