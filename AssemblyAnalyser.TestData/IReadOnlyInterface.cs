using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.TestData
{
    public interface IReadOnlyInterface
    {
        int Id { get; }
    }

    public interface IWriteOnlyInterface
    {
        int Id { set; }
    }

    public interface IReadWriteInterface : IReadOnlyInterface, IWriteOnlyInterface
    {
        
    }

    public class ReadWriteClass : IReadWriteInterface
    {
        public int Id { get; set; }
    }
}
