using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public interface ISpec
    {
        bool IsExcluded();

        bool IsIncluded();
    }
}