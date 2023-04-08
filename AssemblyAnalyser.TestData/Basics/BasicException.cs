using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.TestData.Basics
{
    [NotTested]
    public class BasicException : Exception
    {
        public BasicException()
        {
        }

        public BasicException(string message) : base(message)
        {
        }

        public BasicException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BasicException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
