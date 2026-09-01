using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MBusScaner.ViewModels;

namespace MBusScaner.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = App.Services.GetService<MainViewModel>();
            DataContext = _viewModel;
            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }
    }
}
