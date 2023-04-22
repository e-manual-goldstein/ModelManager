using System.IO;

namespace AssemblyAnalyser
{
    public delegate void ScopeExtendedEventHandler(object sender, ScopeExtendedEventArgs scopeAddedEventArgs);
    
    public class ScopeExtendedEventArgs
    {
        public ScopeExtendedEventArgs(string filePath)
        {
            ScopeDirectory = Path.GetDirectoryName(filePath);
            FilePath = filePath;
        }

        public string ScopeDirectory { get; }
        public string FilePath { get; }
    }
}
