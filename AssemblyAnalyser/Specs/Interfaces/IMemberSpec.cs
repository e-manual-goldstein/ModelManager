using System;

namespace AssemblyAnalyser
{
    public interface IMemberSpec : ISpec, IHasExplicitName
    {
        TypeSpec DeclaringType { get; }
        TypeSpec ResultType { get; }


        
    }

    public interface IAbstractMemberSpec : IMemberSpec
    {
        void RegisterImplementation(IMemberSpec memberSpec);
    }
}