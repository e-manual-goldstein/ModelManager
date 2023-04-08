using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.TestData
{
    internal class NotTestedAttribute : Attribute
    {
        public NotTestedAttribute() 
        {
            //This attribute is used to mark classes and methods which have not been tested
        }
    }
}
