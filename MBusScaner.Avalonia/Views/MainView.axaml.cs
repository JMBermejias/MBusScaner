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
            Opened += (_, _) =>
            {
                if (WindowState != WindowState.Maximized)
                    WindowState = WindowState.Maximized;
            };
        }
    }
}
