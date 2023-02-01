using ModelManager.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ModelManager.Tabs.Outputs
{
    public class SingleOutput : AbstractOutput<string>
    {
        public SingleOutput(string content) : base(content)
        {
            
        }

        public override OutputType OutputType => OutputType.Single;


        public override Control GetOutput(double controlWidth, double tabHeight, out bool success)
        {
            var textBox = new TextBox();
            textBox.TextWrapping = TextWrapping.WrapWithOverflow;
            textBox.AppendText(Content);
            textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _clipboardReady = Content;
            success = true;
            Control = textBox;
            SetControlLayout(controlWidth, tabHeight);
            return textBox;
        }
    }
}
