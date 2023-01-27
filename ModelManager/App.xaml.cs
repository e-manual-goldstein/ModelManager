using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly IServiceProvider _serviceProvider;
        private AppManager _appManager;

        public App()
        {
            _serviceProvider = CreateServiceProvider(new ServiceCollection());            
        }
        public IServiceProvider CreateServiceProvider(ServiceCollection services)
        {
            services.AddSingleton<AppManager>();
            services.AddSingleton<TabManager>();
            services.AddSingleton<MainWindow>();
            services.AddLogging(loggingConfig =>
            {
                loggingConfig.AddDebug();
                loggingConfig.AddConsole();
            });
            return services.BuildServiceProvider();
            
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            _appManager = _serviceProvider.GetService<AppManager>();

        }
    }
}
