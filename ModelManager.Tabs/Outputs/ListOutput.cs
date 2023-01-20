using System;
using System.Collections.Generic;
using System.Text;

namespace ModelManager.Tabs.Outputs
{
    public class ListOutput : AbstractOutput<List<string>>
    {
        public ListOutput(List<string> content) : base(content)
        {

        }
    }
}
