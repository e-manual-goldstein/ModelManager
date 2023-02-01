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
            _textBox = new TextBox()
            {
                TextWrapping = TextWrapping.WrapWithOverflow,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            };
            Control = _textBox;
        }

        TextBox _textBox;

        public override OutputType OutputType => OutputType.Single;


        public override Control GetOutput(double controlWidth, double tabHeight, out bool success)
        {
            _textBox.AppendText(Content);
            _clipboardReady = Content;
            SetControlLayout(controlWidth, tabHeight);
            success = true;
            return _textBox;
        }

        protected override string GetActionableContent()
        {
            if (!string.IsNullOrEmpty(_textBox.SelectedText))
            {
                return _textBox.SelectedText;
            }
            return Content;
        }
    }
}
