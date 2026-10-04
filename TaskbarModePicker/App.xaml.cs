using Microsoft.UI.Xaml;
using TaskbarModePicker.Views;

namespace TaskbarModePicker;

public partial class App : Application
{
    internal static MainWindow? ActiveWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ActiveWindow = new MainWindow();
        ActiveWindow.Activate();
    }
}
