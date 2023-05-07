using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.TestData.Basics
{
    public class BasicSubClass : BasicClass
    {
        public string DeclaredPropertyOnSubClass { get; set; }

        public override void OverridableMethod(int parameter)
        {
            base.OverridableMethod(parameter);
        }

        public void NonOverridableMethod(int parameter)
        {

        }

    }
}
