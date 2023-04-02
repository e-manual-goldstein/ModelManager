using AssemblyAnalyser.TestData.Basics;
using System.Linq;

namespace AssemblyAnalyser.TestData.Complex
{
    public class GenericClass<TGeneric> : IGenericInterface<TGeneric>
    {
        public TGeneric GenericProperty { get; set; } 
    }

    public class GenericClassWithTypeConstraints<TGeneric> : IGenericInterfaceWithTypeConstraints<TGeneric>
        where TGeneric : BasicClass
    {
        public TGeneric GenericProperty { get; set; }
    }

    public class ClassWithGenericMethods : IInterfaceWithGenericMethods
    {
        public TGeneric MethodWithGenericReturnType<TGeneric>()
        {
            return default(TGeneric);
        }

        public void MethodWithGenericTypeConstraints<TGeneric>() where TGeneric : BasicClass
        {
            
        }

        public void MethodWithGenericParameter<TGeneric>(TGeneric generic)
        {
            
        }

        public TSecondGeneric MethodWithMultipleGenericTypeArguments<TFirstGeneric, TSecondGeneric>(TFirstGeneric firstGeneric)
        {
            return default(TSecondGeneric);
        }

        public IQueryable<TNestedGeneric> MethodWithNestedGenericType<TNestedGeneric>()
        {
            return default(IQueryable<TNestedGeneric>);
        }
    }
}
