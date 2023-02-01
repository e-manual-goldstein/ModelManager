using ModelManager.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ModelManager.Tabs.Outputs
{
    public class ListOutput : AbstractOutput<IEnumerable<string>>
    {
        public ListOutput(IEnumerable<string> content) : base(content)
        {

        }

        public override OutputType OutputType => OutputType.List;

        public override Control GetOutput(double controlWidth, double tabHeight, out bool success)
        {
            var listBox = new ListBox() { SelectionMode = SelectionMode.Extended };

            var outputString = new StringBuilder();

            foreach (var line in Content)
            {
                outputString.AppendLine(line);
                listBox.Items.Add(line);
            }
            _clipboardReady = outputString.ToString();
            success = true;
            Control = listBox;
            SetControlLayout(controlWidth, tabHeight);
            return listBox;
        }
    }
}
