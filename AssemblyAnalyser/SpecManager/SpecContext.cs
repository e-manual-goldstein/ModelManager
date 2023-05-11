using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    /// <summary>
    /// The Spec Context object represents a single instance in which the analysis of a set of Specs is taking place.
    /// e.g. A build output for a set of inter-dependent packages
    /// The intended purpose is as a way to avoid conflicting framework versions
    /// </summary>
    public class SpecContext : ISpecContext
    {
        public SpecContext(IAssemblyLocator assemblyLocator) 
        {
            AssemblyLocator = assemblyLocator;
        }

        public IAssemblyLocator AssemblyLocator { get; }
    }
}
