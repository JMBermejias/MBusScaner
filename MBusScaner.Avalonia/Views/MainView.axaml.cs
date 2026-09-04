using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

using MBusScaner.Avalonia.ViewModels;
namespace MBusScaner.Avalonia.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            Opened += async (_, _) =>
            {
                await Task.Delay(100);
                Dispatcher.UIThread.Post(() => WindowState = WindowState.Maximized);
            };
        }
    }
}
