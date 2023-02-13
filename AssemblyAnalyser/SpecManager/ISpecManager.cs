using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface ISpecManager : IAssemblySpecManager, ITypeSpecManager, IMethodSpecManager, IParameterSpecManager, IPropertySpecManager, IFieldSpecManager
    {
        void SetWorkingDirectory(string workingDirectory);
        void ProcessSpecs<TSpec>(IEnumerable<TSpec> specs, bool parallelProcessing = true) where TSpec : AbstractSpec;
        void Reset();
    }
}