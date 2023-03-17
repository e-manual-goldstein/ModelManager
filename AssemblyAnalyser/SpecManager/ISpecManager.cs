using System;
using System.Collections.Generic;
using System.Reflection;

namespace AssemblyAnalyser
{
    public interface ISpecManager : IAssemblySpecManager, IModuleSpecManager, ITypeSpecManager, IMethodSpecManager, IParameterSpecManager, 
        IPropertySpecManager, IFieldSpecManager, IAttributeSpecManager, IEventSpecManager
    {
        void SetWorkingDirectory(string workingDirectory);
        void ProcessSpecs<TSpec>(IEnumerable<TSpec> specs, bool parallelProcessing = true) where TSpec : AbstractSpec;
        void Reset();
        void ProcessAll(bool includeSystem = true, bool parallelProcessing = true);
    }
}