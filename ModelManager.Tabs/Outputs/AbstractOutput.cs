using ModelManager.Core;
using ModelManager.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ModelManager.Tabs.Outputs
{
    public abstract class AbstractOutput<T> : IOutput
    {
        private const double BUTTON_HEIGHT = 30;
        private const double BUTTON_WIDTH = 250;

        protected AbstractOutput(T content) 
        { 
            Content = content;
            ContentActions = new Dictionary<string, Func<T, IOutput>>();
        }

		protected string _clipboardReady;

        protected T Content { get; private set; }

        public abstract OutputType OutputType { get; }

        public List<Button> ActionButtons { get; set; } = new List<Button>();

        public Dictionary<string, Func<T, IOutput>> ContentActions { get; }

        public FrameworkElement Control { get; set; }

        public abstract Control GetOutput(double controlWidth, double tabHeight, out bool success);

        protected virtual void SetControlLayout(double controlWidth, double tabHeight)
        {
            double contentButtonsHeight = 0;
            Control.Width = controlWidth;
            if (ContentActions.Any())
            {
                contentButtonsHeight = AddContentActionButtons();
            }
            Control.MaxHeight = tabHeight - contentButtonsHeight;
        }

        protected abstract T GetActionableContent();

        private double AddContentActionButtons()
        {
            double height = 25;
            foreach (var (key, action) in ContentActions)
            {
                var button = new Button()
                {
                    Content = key,
                    Height = BUTTON_HEIGHT,
                    Width = BUTTON_WIDTH
                };
                                
                button.Click += (sender, eventArgs) =>
                {
                    var actionableContent = GetActionableContent();
                    ActionClicked(() => ContentActions[key](actionableContent), key);
                    //Task.Run(async () => await ActionClickedAsync(() => ContentActions[key](GetActionableContent()), key));
                };
                Canvas.SetBottom(button, height);
                ActionButtons.Add(button);
                height += BUTTON_HEIGHT;
            }
            return height;
        }

        public event ButtonClickedEventHandler ActionClicked;
        public event ButtonClickedAsyncEventHandler ActionClickedAsync;

        public void copyOutput(object sender, RoutedEventArgs e)        
        {
            if (_clipboardReady != null)
                Clipboard.SetText(_clipboardReady);
        }
    }

    public delegate void ButtonClickedEventHandler(Func<object> func, string actionName);
    public delegate Task ButtonClickedAsyncEventHandler(Func<object> func, string actionName);
}
