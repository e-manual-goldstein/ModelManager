using Microsoft.Extensions.DependencyInjection;
using ModelManager.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ModelManager
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
        private readonly ServiceProvider _serviceProvider;
        private AppManager _appManager;

        public App()
        {
            _serviceProvider =
                new ServiceCollection()
                .AddSingleton<AppManager>()
                .AddSingleton<TabManager>()
                .AddSingleton<MainWindow>()
                .BuildServiceProvider();
            
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            _appManager = _serviceProvider.GetService<AppManager>();

        }
    }
}
