using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public interface ISpec
    {
        Task AnalyseAsync(Analyser analyser);
    }
}