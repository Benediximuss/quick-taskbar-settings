using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace TaskbarModePicker;

public sealed partial class MainWindow : Window
{
    private const int PopupWidth = 600;
    private const int PopupHeight = 340;
    private const int PointerGap = 14;

    public bool IsModeChangeInProgress { get; private set; }

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.Title = "Görev çubuğu modu";
        AppWindow.SetIcon("Assets/AppIcon.ico");

        AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        var presenter = AppWindow.Presenter as OverlappedPresenter
            ?? throw new InvalidOperationException("The popup requires an overlapped window presenter.");
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
        PlaceNearPointer();

        RootFrame.Navigate(typeof(MainPage));
        Activated += OnWindowActivated;
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        // Behave like a flyout: clicking elsewhere dismisses the popup.
        if (args.WindowActivationState == WindowActivationState.Deactivated && !IsModeChangeInProgress)
        {
            Close();
        }
    }

    public void SetModeChangeInProgress(bool inProgress)
    {
        IsModeChangeInProgress = inProgress;
    }

    private void PlaceNearPointer()
    {
        if (!GetCursorPos(out var cursor))
        {
            AppWindow.Resize(new SizeInt32(PopupWidth, PopupHeight));
            return;
        }

        var pointer = new PointInt32(cursor.X, cursor.Y);
        var display = DisplayArea.GetFromPoint(pointer, DisplayAreaFallback.Nearest);
        var workArea = display.WorkArea;
        var width = Math.Min(PopupWidth, workArea.Width);
        var height = Math.Min(PopupHeight, workArea.Height);

        var x = cursor.X + PointerGap;
        var y = cursor.Y + PointerGap;

        if (x + width > workArea.X + workArea.Width)
        {
            x = cursor.X - width - PointerGap;
        }

        if (y + height > workArea.Y + workArea.Height)
        {
            y = cursor.Y - height - PointerGap;
        }

        var maxX = Math.Max(workArea.X, workArea.X + workArea.Width - width);
        var maxY = Math.Max(workArea.Y, workArea.Y + workArea.Height - height);
        x = Math.Clamp(x, workArea.X, maxX);
        y = Math.Clamp(y, workArea.Y, maxY);

        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
