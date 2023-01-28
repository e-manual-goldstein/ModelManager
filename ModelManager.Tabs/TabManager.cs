using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelManager.Tabs.Outputs;
using ModelManager.Types;
using ModelManager.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ModelManager.Core
{
    public class TabManager
    {
        #region Private Fields

        private int _defaultTabWidth = 80;
        private int _defaultTabHeight = 26;
        private Thickness _blurredTabThickness = new Thickness(0);
        
        private TabControl _serviceTabControl;
        private List<TabItem> _serviceTabItems = new List<TabItem>();
        private List<AbstractServiceTab> _serviceTabs;

		private TabControl _outputControl;
		private List<TabItem> _outputTabItems = new List<TabItem>();
        private List<OutputTab> _outputTabs = new List<OutputTab>();

        private int _outputTabCount = 0;
        private readonly IServiceProvider _serviceProvider;
        ILogger<TabManager> _logger;
        #endregion

        public Thickness FocusedOutputTabThickness = new Thickness(2, 0, -1, 0);
        public SolidColorBrush ErrorTextBrush = new SolidColorBrush(Colors.Red);


        public TabManager(IServiceProvider serviceProvider, ILogger<TabManager> logger)
        {
            _serviceProvider = serviceProvider;
            _serviceTabs = getServiceTabs();
            _logger = logger;
            createServiceTabs(_serviceTabs);
            initialiseOutputTabs();
            
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;


        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex && _logger != null)
            {
                _logger.LogError(ex, "Unhandled Exception");
            }
        }

        public void DisplayOutput(OutputTab outputTab, object objectToDisplay, AbstractServiceTab source, MethodInfo actionMethod)
		{
			var callingAction = outputTab.ExecutedAction ?? actionMethod;
			outputTab.ClearInputElements();
            outputTab.DisplayOutput(source, callingAction.Name, objectToDisplay ?? "No Output To Display");
            outputTab.Focus();
		}

        public void DisplayInputTab(OutputTab inputTab, AbstractServiceTab executingTab, MethodInfo executedAction)
        {
			inputTab.DisplayInputFields(executingTab, executedAction);
            inputTab.Focus();
        }

        public void DisplayError(Exception exception, AbstractServiceTab source, OutputTab tab)
        {
            tab.TabTitle = "Error";
            var outputString = new StringBuilder();
            outputString.AppendLine("Error calling action. Unwinding Stacktrace:");
            outputString.AppendLine("-------------------------------------------");
            outputString.AppendLine(exception.Message);
            var indent = string.Empty;
            while (exception.InnerException != null)
            {
                indent += " ";
                outputString.AppendLine(indent + ">" + exception.InnerException.Message);
                exception = exception.InnerException;
            }
            tab.DisplayOutput(source, "Error", outputString.ToString());
            tab.Focus();
        }

        #region Service Tabs

        private AbstractServiceTab _activeServiceTab;
        public AbstractServiceTab ActiveServiceTab
        {
            get
            {
                return _activeServiceTab;
            }
            set
            {
                if (_activeServiceTab != null)
                    _activeServiceTab.Blur();
                if (value != null)
                {
                    value.Focus();
                }
                _activeServiceTab = value;
            }
        }

        public TabControl ServiceTabControl
        {
            get
            {
                return _serviceTabControl;
            }
        }

        public List<AbstractServiceTab> ServiceTabs
        {
            get
            {
                return _serviceTabs;
            }
        }

        private void createServiceTabs(List<AbstractServiceTab> tabs)
        {
            _serviceTabControl = new TabControl();
            _serviceTabControl.Margin = new Thickness(0);
            _serviceTabControl.BorderThickness = new Thickness(2);
			var parent = _serviceTabControl.Parent;
			Canvas.SetTop(_serviceTabControl, 0);
            Canvas.SetLeft(_serviceTabControl, 0);
            int width = _defaultTabWidth;
            double left = 0;
            foreach (var tab in tabs)
            {
                addNewServiceTab(tab, width, left, _serviceTabControl);
                left += width;
            }
            ActiveServiceTab = _serviceTabs.First();
        }

        public OutputTab InitialiseOutputTab(AbstractServiceTab executingTab, MethodInfo executedAction)
        {
            var inputTab = createNewOutputTab(_outputControl, ref _outputTabCount, executedAction);
            inputTab.Focus();
            return inputTab;
        }

        private TabItem createNewServiceTabItem(string tabName, double width, double left, TabControl tabControl)
        {
            var tabItem = new TabItem();
            tabItem.Width = width;
            tabItem.Height = _defaultTabHeight;
            tabItem.Header = tabName;
            
            tabItem.Margin = new Thickness(0);
            tabControl.Items.Add(tabItem);
            _serviceTabItems.Add(tabItem);
            return tabItem;
        }

        private void addNewServiceTab(AbstractServiceTab tab, double width, double left, TabControl tabControl)
        {
            var tabItem = createNewServiceTabItem(tab.Title, width, left, tabControl);
            tab.TabManager = this;
            tab.SetControls(tabItem, tabControl);
            tab.InitialiseServiceTab();
        }

        private List<AbstractServiceTab> getServiceTabs()
        {
            var tabList = new List<AbstractServiceTab>();
			var applicationTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
			var tabTypes = applicationTypes
                .Where(c => !c.IsAbstract && c.InheritsFrom(typeof(AbstractServiceTab)))
                .OrderBy(c => (c.GetCustomAttribute(typeof(MemberOrderAttribute)) as MemberOrderAttribute)?.Order ?? int.MaxValue)
                .ToList();
            return tabInstances(tabTypes);
        }

        private List<AbstractServiceTab> tabInstances(List<Type> tabTypes)
        {
            var loggerProvider = _serviceProvider.GetService<ILoggerProvider>();
            var services = tabTypes.Select(t => 
            {
                var instance = Activator.CreateInstance(t) as AbstractServiceTab;
                instance.LoggerProvider = loggerProvider;
                return instance;
            }).ToList();
            return services;
        }
        
        public void FitServiceTabsToWindow(double windowHeight, double windowWidth)
        {
            var workingHeight = windowHeight - 27;
            resizeTabControl(workingHeight, windowWidth * 2 / 5);
        }

		private void resizeTabControl(double newHeight, double newWidth)
        {
            _serviceTabControl.Height = newHeight;
            _serviceTabControl.Width = newWidth;
        }

        #endregion

        #region Output Tabs

        private OutputTab _activeOutputTab;
        public OutputTab ActiveOutputTab
        {
            get
            {
                return _activeOutputTab;
            }
            set
            {
                if (_activeOutputTab != null)
                    _activeOutputTab.Blur();
                if (value != null)
                {
                    value.PreviousTab = _activeOutputTab;
                    value.Focus();
                }
                _activeOutputTab = value;
            }
        }

        public TabControl OutputTabControl
        {
            get
            {
                return _outputControl;
            }
        }

        public List<OutputTab> OutputTabs
        {
            get
            {
                return _outputTabs;
            }
        }



        public void CloseTab(OutputTab outputTab)
        {
            _outputControl.Items.Remove(outputTab.TabItemControl);
        }

        private OutputTab createNewOutputTab(TabControl outputTabControl, ref int tabId, MethodBase callingAction = null)
        {
            tabId++;
            var tabTitle = (callingAction != null) ? AppUtils.CreateDisplayString(callingAction.Name, 12) : "Output - " + tabId;
            var outputTab = new OutputTab(this, _outputControl, tabId, tabTitle);
            outputTab.PreviousTab = ActiveOutputTab;
            ActiveOutputTab = outputTab;
            _outputTabItems.Add(outputTab.TabItemControl);
            _outputTabs.Add(outputTab);
            _outputControl.SelectedItem = outputTab.TabItemControl;
			_outputControl.SizeChanged += outputTab.SizeChanged;
			return outputTab;
        }

        public void ResizeOutputControl(double newHeight, double newWidth)
		{
			_outputControl.Height = newHeight;
			_outputControl.Width = newWidth;
		}

        private void initialiseOutputTabs()
        {
            _outputControl = new TabControl();
			_outputControl.SelectionChanged += selectOutputTab;
			_outputControl.Margin = new Thickness(0);
			_outputControl.TabStripPlacement = Dock.Left;
        }

		private void selectOutputTab(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 0)
			{
				if (e.AddedItems[0] is TabItem)
				{
					var selectedTab = OutputTabs.Single(c => c.TabItemControl == (TabItem)e.AddedItems[0]);
					SelectOutputTab(selectedTab);
				}
			}
			else if (e.RemovedItems.Count != 0)
			{
				if (e.RemovedItems[0] is TabItem)
				{
					var closedTab = OutputTabs.Single(c => c.TabItemControl == (TabItem)e.RemovedItems[0]);
					SelectOutputTab(closedTab.PreviousTab);
				}
			}
		}

		public void SelectOutputTab(OutputTab outputTab)
        {
            ActiveOutputTab = outputTab;
        }
        
        #endregion
    }


}
