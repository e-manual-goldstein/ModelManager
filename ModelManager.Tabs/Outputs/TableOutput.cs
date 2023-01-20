using System;
using System.Collections.Generic;
using System.Text;

namespace ModelManager.Tabs.Outputs
{
    public class TableOutput : AbstractOutput<IDictionary<string, IEnumerable<string>>>
    {
        public TableOutput() : base(new Dictionary<string, IEnumerable<string>>())
        {

        }

        public TableOutput(IDictionary<string, IEnumerable<string>> content) : base(content)
        {

        }

        public void AddColumn(string columnTitle, IEnumerable<string> entries)
        {
            Content[columnTitle] = entries;
        }
    }
}
