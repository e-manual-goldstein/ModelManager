using System.Collections.Generic;

namespace StaticCodeAnalysis
{
	/// <summary>
	/// Role interface for types which either inherit from a super/parent class or implement one or many interface. 
	/// The term "Extension" refers to a declaration of inheritance/implementation in a type definition. 
	/// </summary>
    public interface IHasExtensions
    {
        List<string> Extensions { get; set; }
    }
}