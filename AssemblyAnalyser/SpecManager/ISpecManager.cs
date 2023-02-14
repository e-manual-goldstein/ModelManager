using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface ISpecManager : IAssemblySpecManager, ITypeSpecManager, IMethodSpecManager, IParameterSpecManager, 
        IPropertySpecManager, IFieldSpecManager, IAttributeSpecManager
    {
        void SetWorkingDirectory(string workingDirectory);
        void ProcessSpecs<TSpec>(IEnumerable<TSpec> specs, bool parallelProcessing = true) where TSpec : AbstractSpec;
        void Reset();        
    }
}