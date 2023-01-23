using ModelManager.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ModelManager.Core
{
    public class AppManager
    {
        private TabManager _tabManager;
        private MainWindow _mainWindow;
        private Canvas _baseCanvas;
        private Canvas _toolbarCanvas;
        public const double TOOLBAR_HEIGHT = 30;
        double _defaultWindowHeight = SystemParameters.PrimaryScreenHeight * 0.8;
        double _defaultWindowWidth = SystemParameters.PrimaryScreenWidth * 0.8;
        double _defaultTopMargin = SystemParameters.PrimaryScreenHeight * 0.1;
        double _defaultLeftMargin = SystemParameters.PrimaryScreenWidth * 0.1;

        public AppManager(TabManager tabManager, MainWindow mainWindow)
        {
            _tabManager = tabManager;
            _mainWindow = mainWindow;
            setWindowLayout();
            _baseCanvas = new Canvas();
            _baseCanvas.Background = new SolidColorBrush(Color.FromRgb(212, 208, 200));
            _mainWindow.Content = _baseCanvas;
            createToolbar();
            _mainWindow.SizeChanged += refitComponentsToWindow;
            loadServiceTabs();
			initialiseOutputCanvas();
            _mainWindow.Show();
            //loadBranchControl();
        }
		//public void CreateNewWindow()
		//{
		//	var newWindow = new Window();
		//	newWindow.Height = 500;
		//	newWindow.Width = 600;
		//	newWindow.Show();
		//}
		private void loadServiceTabs()
        {
            AddToWindow(_tabManager.ServiceTabControl);
            _tabManager.FitServiceTabsToWindow(_mainWindow.Height, _mainWindow.Width - 8);
			foreach (var tabService in _tabManager.ServiceTabs)
			{
				tabService.LoadActionButtons();
			}
		}

		private void initialiseOutputCanvas()
		{
			AddToWindow(_tabManager.OutputTabControl);
            Canvas.SetRight(_tabManager.OutputTabControl, 0);
            Canvas.SetTop(_tabManager.OutputTabControl, 28);
			FitOutputControlToWindow(_mainWindow.Height, _mainWindow.Width);
		}

        private void refitComponentsToWindow(object sender, SizeChangedEventArgs e)
        {
            var newHeight = e.NewSize.Height;
            var newWidth = e.NewSize.Width;
            _toolbarCanvas.Width = newWidth;
            FitOutputControlToWindow(newHeight, newWidth);

            _tabManager.FitServiceTabsToWindow(newHeight, newWidth);
        }
        private void createToolbar()
        {
            _toolbarCanvas = new Canvas();
            _toolbarCanvas.Width = _mainWindow.Width;
            _toolbarCanvas.Height = TOOLBAR_HEIGHT;
            //_toolbarCanvas.Background = new SolidColorBrush(Colors.BurlyWood);
            _baseCanvas.Children.Add(_toolbarCanvas);
        }
        public void FitOutputControlToWindow(double windowHeight, double windowWidth)
        {
            var toolbarHeight = TOOLBAR_HEIGHT;
            var workingWidth = windowWidth - 10;
            var workingHeight = windowHeight - toolbarHeight - 24;
            _tabManager.ResizeOutputControl(workingHeight, workingWidth * 3 / 5);
        }


        public string WorkingBranch { get; set; }

        public ComboBox BranchSelector { get; set; }

        //private Dictionary<string, string> localBranches;


        private void setWindowLayout()
        {
            useDefaultLayout();            
        }

        private void useDefaultLayout()
        {
            _mainWindow.Height = _defaultWindowHeight;
            _mainWindow.Width = _defaultWindowWidth;
            _mainWindow.Top = _defaultTopMargin;
            _mainWindow.Left = _defaultLeftMargin;
        }

        public void AddToWindow(UIElement element, double top = 0, double left = 0)
        {
            if (top != 0)
                Canvas.SetTop(element, top);
            if (left != 0)
                Canvas.SetLeft(element, left);
            _baseCanvas.Children.Add(element);
        }

    }
}
