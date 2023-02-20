using System;

namespace AssemblyAnalyser.TestData
{
    [Basic]
    public class BasicClass : IBasicInterface
    {
        public string PublicProperty { get; set; }

        [Basic]
        public DateTime PublicMethod()
        {
            return DateTime.Now;
        }

        public void PublicMethodWithParameters(string stringParam, Guid guidParam)
        {

        }

        public string ReadOnlyInterfaceImpl { get; }

        public string ReadWriteInterfaceImpl { get; set; }

        public int PublicField;

        public event BasicDelegate BasicEvent;
    }
}
