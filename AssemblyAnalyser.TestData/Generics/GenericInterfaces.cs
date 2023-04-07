using AssemblyAnalyser.TestData.Basics;
using System.Linq;

namespace AssemblyAnalyser.TestData.Generics
{
    public interface IGenericInterface<TGenericForInterface> 
    {
        TGenericForInterface GenericProperty { get; set; }
    }

    public interface IGenericInterfaceWithTypeConstraints<TGenericForInterface> where TGenericForInterface : BasicClass
    {
        public TGenericForInterface GenericProperty { get; set; }
    }

    public interface IInterfaceWithGenericMethods
    {
        public TGenericForMethod MethodWithGenericReturnType<TGenericForMethod>();

        public void MethodWithGenericTypeConstraints<TGenericForMethod>() where TGenericForMethod : BasicClass;

        public void MethodWithGenericParameter<TGenericForMethod>(TGenericForMethod generic);

        public TSecondGenericForMethod MethodWithMultipleGenericTypeArguments<TFirstGenericForMethod, TSecondGenericForMethod>(TFirstGenericForMethod firstGeneric);

        public IQueryable<TNestedGeneric> MethodWithNestedGenericType<TNestedGeneric>();
    }
}
