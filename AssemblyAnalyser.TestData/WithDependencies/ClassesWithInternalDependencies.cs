using AssemblyAnalyser.TestData.Basics;
using System;

namespace AssemblyAnalyser.TestData.WithInternalDependencies
{
    [NotTested]
    public class ClassWithMethodBodyDependency
    {        
        public string MethodWithDependencyInBody()
        {
            var newObject = new BasicClass();
            return newObject.PublicProperty;
        }        
    }

    [NotTested]
    public class ClassWithMethodParameterDependency
    {
        public void MethodWithParameterDependency(BasicClass basicClass)
        {
            
        }
    }

    [NotTested]
    public class ClassWithMethodReturnTypeDependency
    {
        public BasicClass MethodWithReturnTypeDependency()
        {
            throw new NotImplementedException();
        }
    }

    [NotTested]
    public class ClassWithMethodGenericTypeParameterDependency
    {
        public void MethodWithGenericTypeParameterDependency<TGeneric>() where TGeneric : BasicClass
        {

        }
    }

    [NotTested]
    public class ClassWithInterfaceDependency : IReadWriteInterface
    {
        public int Id { get; set; }
    }

    [NotTested]
    public class ClassWithPropertyDependency
    {
        public BasicClass BasicClassProperty { get; set; }
    }

    [NotTested]
    public class ClassWithFieldDependency
    {
        public BasicClass BasicClassField;
    }

    [NotTested]
    public class ClassWithEventDependency
    {
        public event BasicDelegate BasicEvent;
    }
}