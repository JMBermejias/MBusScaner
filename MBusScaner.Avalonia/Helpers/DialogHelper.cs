using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;

namespace MBusScaner.Avalonia.Helpers
{
    public static class DialogHelper
    {
        private static Window? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;
            return null;
        }

        public static async Task ShowMessage(string title, string message)
        {
            var window = GetMainWindow();
            if (window == null) return;

            var dialog = new Window
            {
                Title = title,
                Width = 420, Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Children =
                    {
                        new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 14, Margin = new Thickness(0,0,0,16) },
                        new Button { Content = "Aceptar", Classes = { "primary" }, HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 100 }
                    }
                }
            };

            var btn = (Button)((StackPanel)dialog.Content!).Children[1];
            btn.Click += (_, _) => dialog.Close();
            await dialog.ShowDialog(window);
        }

        public static async Task<bool> ShowConfirm(string title, string message)
        {
            var window = GetMainWindow();
            if (window == null) return false;

            bool result = false;
            var dialog = new Window
            {
                Title = title,
                Width = 420, Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Children =
                    {
                        new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 14, Margin = new Thickness(0,0,0,16) },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Children =
                            {
                                new Button { Content = "Cancelar", Classes = { "secondary" }, MinWidth = 100 },
                                new Button { Content = "Aceptar", Classes = { "primary" }, MinWidth = 100 }
                            }
                        }
                    }
                }
            };

            var panel = (StackPanel)((StackPanel)dialog.Content!).Children[1];
            var cancelBtn = (Button)panel.Children[0];
            var okBtn = (Button)panel.Children[1];

            cancelBtn.Click += (_, _) => { result = false; dialog.Close(); };
            okBtn.Click += (_, _) => { result = true; dialog.Close(); };
            await dialog.ShowDialog(window);
            return result;
        }
    }
}