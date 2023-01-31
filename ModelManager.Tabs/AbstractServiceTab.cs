using Microsoft.Extensions.Logging;
using ModelManager.Tabs;
using ModelManager.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ModelManager.Core
{
    public abstract class AbstractServiceTab : IServiceTab, IOutputSource
    {
        public abstract string Title { get; }
        public ILoggerProvider LoggerProvider { protected get; set; }
        
        private TextBox _console;
        private TabControl _tabControl;
        private TabItem _tabItemControl;
        private TabManager _tabManager;
        private Canvas _tabCanvas = new Canvas();
        private Canvas _buttonCanvas;
        private Dictionary<string, MethodInfo> _actionMethods = new Dictionary<string, MethodInfo>();
        //private Dictionary<string, string> _actionSummaries = new Dictionary<string, string>();
        private const double BUTTON_HEIGHT = 30;
        private const double BUTTON_WIDTH = 250;
        private Thickness _focusedServiceTabThickness = new Thickness(0, 2, 0, -1);

        public void Focus()
        {
            _tabItemControl.Margin = _focusedServiceTabThickness;
        }

        public void Blur()
        {
            _tabItemControl.Margin = new Thickness(0);
        }

        public TabManager TabManager
        {
            get { return _tabManager; }
            set { _tabManager = value; }
        }

        public void SetControls(TabItem tabItemControl, TabControl tabControl)
        {
            if (_tabItemControl != null || _tabControl != null)
                throw new InvalidOperationException("Service Tab already bound to another Control");
            else
            {
                _tabItemControl = tabItemControl;
                _tabControl = tabControl;
            }
        }

        public void InitialiseServiceTab()
        {
            //loadConsole();
            _tabControl.SizeChanged += tabControl_SizeChanged;
            _tabItemControl.Content = _tabCanvas;
            _tabItemControl.MouseLeftButtonUp += selectServiceTab;
            drawButtonPanel();
        }

        public void LoadActionButtons()
        {
            int buttonId = 0;
            foreach (var action in DefinedActions)
            {
                buttonId++;
                createNewButtonFromAction(action, buttonId);
            }
        }

        private void drawButtonPanel()
        {
            _buttonCanvas = new Canvas();
            Canvas.SetTop(_buttonCanvas, 25);
            Canvas.SetLeft(_buttonCanvas, 25);
            _tabCanvas.Children.Add(_buttonCanvas);
        }

        private Button createNewButtonFromAction(MethodInfo action, int buttonId)
        {
            string actionId = Title + "_" + buttonId;
            var actionButton = new Button();
            actionButton.Name = actionId;
            actionButton.Click += clickButton;
            _actionMethods.Add(actionId, action);
            actionButton.Height = BUTTON_HEIGHT;
            actionButton.Width = BUTTON_WIDTH;
            actionButton.Content = AppUtils.CreateDisplayString(action.Name);
            placeButtonOnTab(actionButton, buttonId);
            return actionButton;
        }

        //TODO: This won't work in its current form
        //private Button createInfoButtonForAction(MethodInfo action, int buttonId)
        //{
        //    string infoId = Title + "_" + buttonId + "_Info";
        //    var infoButton = new Button();
        //    infoButton.Name = infoId;
        //    infoButton.Click += clickButton;
        //    var summary = action.
        //    _actionSummaries.Add(infoId, summary);
        //    infoButton.Height = BUTTON_HEIGHT;
        //    infoButton.Width = BUTTON_WIDTH;
        //    infoButton.Content = AppUtils.CreateDisplayString(action.Name);
        //    placeButtonOnTab(infoButton, buttonId);
        //    return infoButton;
        //}

        private void placeButtonOnTab(Button button, int buttonCount)
        {
            var top = ((buttonCount - 1) * BUTTON_HEIGHT) + 1;
            Canvas.SetTop(button, top);
            _buttonCanvas.Children.Add(button);
        }

        protected async void clickButton(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var buttonTitle = button.Name;
                var actionMethod = _actionMethods[buttonTitle];
                if (actionMethod != null)
                {
                    var tab = _tabManager.InitialiseOutputTab(this, actionMethod);
                    if (actionMethod.GetParameters().Any())
                        _tabManager.DisplayInputTab(tab, this, actionMethod);
                    else
                    {
                        try
                        {
							tab.DisplayExecutingMessage();
							var task = Task.Run(() => InvokeAction(actionMethod, new object[] { }));
                            await task;
                            _tabManager.DisplayOutput(tab, task.Result, this, actionMethod);
                        }
                        catch (Exception ex)
                        {
                            _tabManager.DisplayError(ex, this, tab);
                        }
                    }
                }
            }
        }

        public object InvokeAction(MethodInfo actionMethod, object[] parameters)
        {   
            return actionMethod.Invoke(this, parameters);
        }

		private void loadConsole()
        {
            _console = new TextBox() { Width = 700 };
            _tabCanvas.Children.Add(_console);
            Canvas.SetRight(_console, 0);
        }

        private void tabControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            resizeConsole(e.NewSize.Height - _tabItemControl.Height - 10);
        }

        private void resizeConsole(double consoleHeight)
        {
            if (_console != null)
                _console.Height = consoleHeight;
        }

        private List<MethodInfo> _definedActions;

        public List<MethodInfo> DefinedActions
        {
            get
            {
                if (_definedActions == null)
                    _definedActions = getDefinedActions();
                return _definedActions;
            }
        }

        private List<MethodInfo> getDefinedActions()
        {
            var allMethods = GetType().GetMethods();
            return allMethods.Where(meth => meth.IsPublic && !meth.IsSpecialName && meth.DeclaringType == GetType()).ToList();
        }

        public void WriteLine(object entry)
        {
            _console.AppendText(entry.ToString() + Environment.NewLine);
        }

        public void ListView(IEnumerable inputList)
        {
            foreach (var input in inputList)
            {
                WriteLine(input);
            }
        }

		[Obsolete]
        public void TableView(IEnumerable[] inputTable)
        {
            foreach (IEnumerable enumerable in inputTable)
            {
                foreach (object item in enumerable)
                {

                }
            }
        }

        private void selectServiceTab(object sender, RoutedEventArgs e)
        {
            TabManager.ActiveServiceTab = this;
        }
    }
}
