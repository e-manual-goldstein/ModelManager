using System;

namespace AssemblyAnalyser.TestData.Complex
{
    [NotTested]
    public class ClassWithAnonymousTypeInMethodBody
    {
        public string MethodWithAnonymousTypeInBody()
        {
            var newObject = new { PublicProperty = "PublicString", AnotherProperty = 42 };
            return newObject.PublicProperty;
        }
    }
    
    [NotTested]
    public class ClassWithLambdaInMethodBody
    {
        public string MethodWithLambdaInBody()
        {
            var newObject = () =>
            {
                return string.Empty;
            };
            return newObject();
        }
    }

    [NotTested]
    public class ClassWithLocalMethodInMethodBody
    {
        public string MethodWithLocalMethodInBody()
        {
            string GetString()
            {
                return string.Empty;
            }
            return GetString();
        }
    }

    [NotTested]
    public class ClassWithMethodThatThrowsException
    {
        public string MethodWhichThrowsException()
        {
            throw new Exception();
        }
    }
    
    [NotTested]
    public class ClassWithTryCatchFinallyBlocksInMethodBodies
    {
        public void MethodWithTryCatchBlockInBody()
        {
            try
            {

            }
            catch
            {

            }            
        }

        public string MethodWithSpecificCaughtExceptionInBody()
        {
            try
            {

            }
            catch (NotImplementedException ex)
            {
                return ex.Message;
            }
            return string.Empty;
        }

        public string MethodWithMultipleCatchBlocksInBody()
        {
            try
            {

            }
            catch (NotImplementedException ex)
            {
                return ex.Message;
            }
            catch (NotSupportedException ex)
            {
                return ex.Message;
            }
            catch
            {
                
            }
            finally
            {
                
            }
            return string.Empty;
        }

        public string MethodWithFinallyBlockInBody()
        {
            try
            {

            }
            catch (NotImplementedException ex)
            {
                return ex.Message;
            }
            finally
            {

            }
            return string.Empty;
        }
    }
}
