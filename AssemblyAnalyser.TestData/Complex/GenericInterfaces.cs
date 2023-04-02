using AssemblyAnalyser.TestData.Basics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.TestData.Complex
{
    public interface IGenericInterface<TGeneric> 
    {
        TGeneric GenericProperty { get; set; }
    }

    public interface IGenericInterfaceWithTypeConstraints<TGeneric> where TGeneric : BasicClass
    {
        public TGeneric GenericProperty { get; set; }
    }

    public interface IInterfaceWithGenericMethods
    {
        public TGeneric MethodWithGenericReturnType<TGeneric>();

        public void MethodWithGenericTypeConstraints<TGeneric>() where TGeneric : BasicClass;

        public void MethodWithGenericParameter<TGeneric>(TGeneric generic);

        public TSecondGeneric MethodWithMultipleGenericTypeArguments<TFirstGeneric, TSecondGeneric>(TFirstGeneric firstGeneric);

        public IQueryable<TNestedGeneric> MethodWithNestedGenericType<TNestedGeneric>();
    }
}
