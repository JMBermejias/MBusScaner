using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MBusScaner.ViewModels;
using MBusScaner.Views;

namespace MBusScaner
{
    public partial class App : Application
    {
        public static ServiceProvider Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<ScanViewModel>();
            services.AddTransient<DeviceDetailViewModel>();
            services.AddTransient<SettingsViewModel>();
            Services = services.BuildServiceProvider();

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
