
using ModelManager.Types;
using StaticCodeAnalysis;
using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GlobalClass
{

}

namespace TestNamespace.Other
{
	[MemberOrder(0.0), Test]
    // This is an example code file used for static code analysis testing

		/* 
		*	These
		//	Lines
			All 
			Contain
			Comments
		 */
    public class TestCodeClass : OtherTestCodeClass, IHasExtensions, ITestInterface
    {
        public class NestedType
        {
        }

		#region Test Region

		public string virtualString;


        const string thisIsAPrivateString = "This is an initialised string";
        readonly string uninitialisedField;
		private const string ContainedOutcomeBase64 = "AAEAAAD//";

		#endregion

		List<string> initialisedStringList = new List<string>() { "Hello" };

		
		public void FuncMethod(
			List
			<
			string
			> stri
			) 
		{
			uninitialisedField = "";
			var t = uninitialisedField;
		}

        public List<string> Extensions { get; set; }
		
		public string MakeAStringFromAMethod()
		{
			
		}
		
		public string MakeAnotherStringFromAMethod()
		{
			
		}


        public string GetString(string optionalString = "")
        {
            return thisIsAPrivateString;
        }
    }

    [Test]
    [MemberOrder(0.5)]
    public class OtherTestCodeClass
    {

    }
}


namespace TestNamespace
{
	public class OtherTestCodeClass
    {

    }

    public interface ITestInterface
    {

    }

    [MemberOrder(0.0)]
    public struct TestStruct : ITestInterface
    {
        public List<string> StringList { get; set; }

        public OtherTestCodeClass TestProperty { get; set; }

        public struct OtherStruct
        {

        }

		
    }

	public class TestGenericType<T> where T : class
	{
		public TestGenericType()
		{
		}

	}


	[Test]
    public enum TestEnum
    {

    }
}