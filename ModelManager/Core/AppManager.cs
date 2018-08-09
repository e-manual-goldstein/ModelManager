using ModelManager.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ModelManager.Core
{
    public class AppManager
    {
        private TabManager _tabManager;
        private WindowManager _windowManager;

		public AppManager(TabManager tabManager, WindowManager windowManager)
        {
            _tabManager = tabManager;
            _windowManager = windowManager;
            loadServiceTabs();
			initialiseOutputCanvas();
            //loadBranchControl();
        }

        private void loadServiceTabs()
        {
            _windowManager.AddToWindow(_tabManager.ServiceTabControl);
            _tabManager.FitServiceTabsToWindow(_windowManager.WindowHeight, _windowManager.WindowWidth - 8);
			foreach (var tabService in _tabManager.ServiceTabs)
			{
				tabService.LoadActionButtons();
			}
		}

		private void initialiseOutputCanvas()
		{
			_windowManager.AddToWindow(_tabManager.OutputTabControl);
            Canvas.SetRight(_tabManager.OutputTabControl, 0);
            Canvas.SetTop(_tabManager.OutputTabControl, 28);
			_windowManager.FitOutputControlToWindow(_windowManager.WindowHeight, WindowManager.WindowWidth);
		}

        //private void loadBranchControl()
        //{
        //    //getLocalBranches();
        //    BranchSelector = new ComboBox();
        //    BranchSelector.Width = 100;
        //    BranchSelector.Items.Add("TestBranch");
        //    WindowManager.AddToWindow(BranchSelector, 5, WindowManager.WindowWidth * 2 / 5);
        //}

        public string WorkingBranch { get; set; }

        public ComboBox BranchSelector { get; set; }

        //private Dictionary<string, string> localBranches;

        private void getLocalBranches(bool forceReload = false)
        {
            //if (AppUtils.IsFirstRun() || forceReload)
            //    TFUtils.LookupBranchesOnline();
        }

        public TabManager TabManager
		{
			get
			{
				return _tabManager;
			}
		}

		public WindowManager WindowManager
		{
			get
			{
				return _windowManager;
			}
		}
    }
}
