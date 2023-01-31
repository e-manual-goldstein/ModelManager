using ModelManager.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelManager.Tabs.Outputs
{
    public class ListOutput : AbstractOutput<IEnumerable<string>>
    {
        public ListOutput(IEnumerable<string> content) : base(content)
        {

        }

        public override OutputType OutputType => OutputType.List;
    }
}
