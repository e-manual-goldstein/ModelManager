using System;

namespace AssemblyAnalyser
{
    public interface ISpecManager : IAssemblySpecManager, ITypeSpecManager, IMethodSpecManager, IParameterSpecManager, IPropertySpecManager, IFieldSpecManager
    {

        void SetWorkingDirectory(string workingDirectory);
    }
}