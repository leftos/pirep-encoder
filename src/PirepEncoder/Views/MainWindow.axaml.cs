using Avalonia.Controls;
using Avalonia.Interactivity;
using PirepEncoder.ViewModels;

namespace PirepEncoder.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnOpenSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel main)
        {
            return;
        }
        var dialog = new SettingsWindow
        {
            DataContext = main.Settings,
        };
        await dialog.ShowDialog(this);
        main.PersistSettingsCommand.Execute(null);
    }
}
