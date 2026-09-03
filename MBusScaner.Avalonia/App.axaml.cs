using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MBusScaner.Avalonia.Views;
using MBusScaner.Avalonia.ViewModels;

namespace MBusScaner.Avalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var mainVm = new MainViewModel();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainView { DataContext = mainVm };
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}
