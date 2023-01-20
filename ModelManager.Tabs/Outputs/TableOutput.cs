using System;
using System.Collections.Generic;
using System.Text;

namespace ModelManager.Tabs.Outputs
{
    public class TableOutput : AbstractOutput<Dictionary<string, IEnumerable<string>>>
    {
        public TableOutput() : base(new Dictionary<string, IEnumerable<string>>())
        {

        }

        public void AddColumn(string columnTitle, IEnumerable<string> entries)
        {
            Content[columnTitle] = entries;
        }
    }
}
