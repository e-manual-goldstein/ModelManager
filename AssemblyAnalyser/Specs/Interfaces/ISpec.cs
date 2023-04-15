using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public interface ISpec : IHasName
    {
        bool IsExcluded();

        bool IsIncluded();

        bool IsSystem { get; }
    }
}