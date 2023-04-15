using AssemblyAnalyser.TestData.Basics;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser.TestData.Generics
{
    [NotTested]
    public interface IGenericInterface<TGenericForInterface> 
    {
        TGenericForInterface GenericProperty { get; set; }
    }

    [NotTested]
    public interface IGenericInterfaceWithTypeConstraints<TGenericForInterface> where TGenericForInterface : BasicClass
    {
        public TGenericForInterface GenericProperty { get; set; }
    }

    [NotTested]
    public interface IInterfaceWithGenericMethods
    {
        public TGenericForInterfaceMethod MethodWithGenericReturnType<TGenericForInterfaceMethod>();

        public void MethodWithGenericTypeConstraints<TGenericForInterfaceMethod>() where TGenericForInterfaceMethod : BasicClass;

        public void MethodWithGenericParameter<TGenericForInterfaceMethod>(TGenericForInterfaceMethod generic);

        public TSecondGenericForMethod MethodWithMultipleGenericTypeArguments<TFirstGenericForInterfaceMethod, TSecondGenericForMethod>(TFirstGenericForInterfaceMethod firstGeneric);

        public IQueryable<TNestedGenericForInterface> MethodWithNestedGenericType<TNestedGenericForInterface>();
    }

    public interface IInterfaceWithSynonymousMethods
    {
        public List<IBasicInterface> SynonymousMethod<TLink, THolder>(THolder holder,
           params int[] excludedNoteTypes)
           where TLink : class, IGenericInterface<THolder>
           where THolder : class, IReadOnlyInterface;

        public List<IBasicInterface> SynonymousMethod<TLink, THolder>(THolder holder)
            where TLink : class, IGenericInterface<THolder>
            where THolder : class, IReadOnlyInterface;
    }
}
