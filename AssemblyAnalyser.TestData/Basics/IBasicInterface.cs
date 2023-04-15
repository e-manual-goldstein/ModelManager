using System;

namespace AssemblyAnalyser.TestData.Basics
{
    public interface IBasicInterface
    {
        string ReadOnlyInterfaceImpl { get; }
        string ReadWriteInterfaceImpl { get; set; }

        DateTime PublicMethod();

        void PublicMethodWithParameters(string stringParam, Guid guidParam);

        DateTime PropertyForExplicitImplementation { get; }

        DateTime MethodForExplicitImplementation();
    }
}