using System;

namespace AssemblyAnalyser.TestData.Basics
{
    [Basic]
    public class BasicClass : IBasicInterface
    {

        public BasicClass()
        {

        }

        public BasicClass(string stringParam)
        {

        }

        public string PublicProperty { get; set; }

        public string[] ArrayProperty { get; set; }

        public string PropertyWithUniqueName { get; set; }

        public void MethodWithUniqueName(int property)
        {

        }

        public string this[int index]
        {
            get
            {
                return ArrayProperty[index];
            }
        }

        [Basic]
        public DateTime PublicMethod()
        {
            return DateTime.Now;
        }

        public void PublicMethodWithArrayParameter(string[] stringParam)
        {

        }

        public void PublicMethodWithParameters(string stringParam, Guid guidParam)
        {

        }

        public void MethodWithOutParameter(string stringParam, Guid guidParam, out string outParam)
        {
            outParam = stringParam;
        }

        public void OverloadedMethod(string stringParam, Guid guidParam, out string outParam)
        {
            outParam = stringParam;
        }

        public void OverloadedMethod(string stringParam, Guid guidParam, string outParam)
        {
            
        }

        public void OverloadedMethod(string stringParam, string outParam)
        {
            
        }

        public void OverloadedMethod(string stringParam, Guid guidParam)
        {
            
        }

        public string ReadOnlyInterfaceImpl { get; }

        public string ReadWriteInterfaceImpl { get; set; }

        DateTime IBasicInterface.PropertyForExplicitImplementation { get; }

        DateTime IBasicInterface.MethodForExplicitImplementation()
        {
            return default;
        }

        public int PublicField;

        public event BasicDelegate BasicEvent;
    }
}
