using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Configuration;
using System.Windows.Controls;
using ModelManager.Utils;
using System.Windows.Media;

namespace ModelManager.Core
{
    public class WindowManager
    {
        private MainWindow _mainWindow;
        double _defaultWindowHeight = SystemParameters.PrimaryScreenHeight * 0.8;
        double _defaultWindowWidth = SystemParameters.PrimaryScreenWidth * 0.8;
        double _defaultTopMargin = SystemParameters.PrimaryScreenHeight * 0.1;
        double _defaultLeftMargin = SystemParameters.PrimaryScreenWidth * 0.1;
        private Canvas _baseCanvas;
        private Canvas _toolbarCanvas;
        public const double TOOLBAR_HEIGHT = 30;

        public WindowManager(MainWindow mainWindow)
		{
			_mainWindow = mainWindow;
			setWindowLayout();
			_baseCanvas = new Canvas();
            _baseCanvas.Background = new SolidColorBrush(Color.FromRgb(212, 208, 200));
			_mainWindow.Content = _baseCanvas;
            createToolbar();
            _mainWindow.SizeChanged += refitComponentsToWindow;
		}

        private void refitComponentsToWindow(object sender, SizeChangedEventArgs e)
        {
            var newHeight = e.NewSize.Height;
            var newWidth = e.NewSize.Width;
            _toolbarCanvas.Width = newWidth;
            FitOutputControlToWindow(newHeight, newWidth);
            App.Manager.TabManager.FitServiceTabsToWindow(newHeight, newWidth);
        }

		public void FitOutputControlToWindow(double windowHeight, double windowWidth)
		{
			var toolbarHeight = TOOLBAR_HEIGHT;
			var workingWidth = windowWidth - 10;
			var workingHeight = windowHeight - toolbarHeight - 24;
			if (App.Manager != null)
				App.Manager.TabManager.ResizeOutputControl(workingHeight, workingWidth * 3 / 5);
		}

		private void createToolbar()
        {
            _toolbarCanvas = new Canvas();
            _toolbarCanvas.Width = WindowWidth;
            _toolbarCanvas.Height = TOOLBAR_HEIGHT;
            //_toolbarCanvas.Background = new SolidColorBrush(Colors.BurlyWood);
            _baseCanvas.Children.Add(_toolbarCanvas);
        }

        public void AddToWindow(UIElement element, double top = 0, double left = 0)
        {
            if (top != 0)
                Canvas.SetTop(element, top);
            if (left != 0)
                Canvas.SetLeft(element, left);
            _baseCanvas.Children.Add(element);
        }

        private void setWindowLayout()
        {
            if (AppUtils.IsFirstRun()) useDefaultLayout();
            else useDefinedLayout();
        }

        private void useDefaultLayout()
        {
            _mainWindow.Height = _defaultWindowHeight;
            _mainWindow.Width = _defaultWindowWidth;
            _mainWindow.Top = _defaultTopMargin;
            _mainWindow.Left = _defaultLeftMargin;
        }

        private void useDefinedLayout()
        {
            _mainWindow.Height = AppUtils.GetAppSetting<double>("WindowHeight");
            _mainWindow.Width = AppUtils.GetAppSetting<double>("WindowWidth");
            _mainWindow.Top = AppUtils.GetAppSetting<double>("WindowTopMargin");
            _mainWindow.Left = AppUtils.GetAppSetting<double>("WindowLeftMargin");
        }

        public double WindowHeight
        {
            get
            {
                return _mainWindow.Height;
            }
        }

        public double WindowWidth
        {
            get
            {
                return _mainWindow.Width;
            }
        }

        
    }
}
