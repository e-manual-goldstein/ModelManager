using AssemblyAnalyser.TestData.Basics;
using System.Linq;

namespace AssemblyAnalyser.TestData.Generics
{
    public class ClassImplementingGenericInterface : IGenericInterface<BasicClass>
    {
        public BasicClass GenericProperty { get; set; }
    }

    public class GenericClass<TGenericForClass> : IGenericInterface<TGenericForClass>
    {
        public TGenericForClass GenericProperty { get; set; } 
    }

    public class GenericClassWithClassTypeConstraints<TGenericForClass> : IGenericInterfaceWithTypeConstraints<TGenericForClass>
        where TGenericForClass : BasicClass
    {
        public TGenericForClass GenericProperty { get; set; }
    }

    public class GenericClassWithDefaultConstructorTypeConstraints<TGenericForClass>
        where TGenericForClass : new()
    {
        public TGenericForClass GenericProperty { get; set; }
    }

    public class ClassWithGenericMethods : IInterfaceWithGenericMethods
    {
        public GenericClass<BasicClass> MethodWithGenericTypeInstanceAsReturnType()
        {
            return default(GenericClass<BasicClass>);
        }

        public TGenericForMethod MethodWithGenericReturnType<TGenericForMethod>()
        {
            return default(TGenericForMethod);
        }

        public void MethodWithGenericTypeConstraints<TGenericForMethod>() where TGenericForMethod : BasicClass
        {
            
        }

        public void MethodWithGenericParameter<TGenericForMethod>(TGenericForMethod generic)
        {
            
        }

        public TSecondGenericForMethod MethodWithMultipleGenericTypeArguments<TFirstGenericForMethod, TSecondGenericForMethod>(TFirstGenericForMethod firstGeneric)
        {
            return default(TSecondGenericForMethod);
        }

        public IQueryable<TNestedGeneric> MethodWithNestedGenericType<TNestedGeneric>()
        {
            return default(IQueryable<TNestedGeneric>);
        }
    }
}
