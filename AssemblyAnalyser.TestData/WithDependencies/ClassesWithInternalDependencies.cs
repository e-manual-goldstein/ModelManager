using AssemblyAnalyser.TestData.Basics;
using System;

namespace AssemblyAnalyser.TestData.WithInternalDependencies
{
    public class ClassWithMethodBodyDependency
    {        
        public string MethodWithDependencyInBody()
        {
            var newObject = new BasicClass();
            return newObject.PublicProperty;
        }        
    }

    public class ClassWithMethodParameterDependency
    {
        public void MethodWithParameterDependency(BasicClass basicClass)
        {
            
        }
    }

    public class ClassWithMethodReturnTypeDependency
    {
        public BasicClass MethodWithReturnTypeDependency()
        {
            throw new NotImplementedException();
        }
    }

    public class ClassWithMethodGenericTypeParameterDependency
    {
        public void MethodWithGenericTypeParameterDependency<TGeneric>() where TGeneric : BasicClass
        {

        }
    }

    public class ClassWithInterfaceDependency : IReadWriteInterface
    {
        public int Id { get; set; }
    }

    public class ClassWithPropertyDependency
    {
        public BasicClass BasicClassProperty { get; set; }
    }

    public class ClassWithFieldDependency
    {
        public BasicClass BasicClassField;
    }

    public class ClassWithEventDependency
    {
        public event BasicDelegate BasicEvent;
    }
}
