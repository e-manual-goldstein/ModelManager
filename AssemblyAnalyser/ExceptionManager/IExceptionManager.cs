

using System;
using System.Collections.Generic;

namespace AssemblyAnalyser
{
    public interface IExceptionManager
    {
        void Handle(Exception exception);

        List<string> MissingFiles { get; }
    }
}