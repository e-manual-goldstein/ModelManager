using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class ExceptionManager : IExceptionManager
    {
        public List<string> MissingFiles { get; set; } = new List<string>();

        public void Handle(Exception exception)
        {
            if (exception is FileNotFoundException fileNotFoundException)
            {
                var missingFile = fileNotFoundException.FileName ?? fileNotFoundException.Message;
                if (!MissingFiles.Contains(missingFile))
                {
                    MissingFiles.Add(missingFile);
                }
            }
            else
            {

            }
        }
    }
}
