using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using TaskbarModePicker.Resources;
using WinRT.Interop;
using Windows.Graphics;

namespace TaskbarModePicker.Views;

public sealed partial class MainWindow : Window
{
    private const int PopupWidth = 600;
    private const int PopupHeight = 340;
    private const int EdgeMargin = 8;

    public bool IsModeChangeInProgress { get; private set; }

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.Title = AppStrings.Get("TaskbarModePickerTitle");
        AppWindow.SetIcon("Assets/AppIcon.ico");

        AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        var presenter = AppWindow.Presenter as OverlappedPresenter
            ?? throw new InvalidOperationException("The popup requires an overlapped window presenter.");
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);
        PlaceAtBottomRight();

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

    public void ReactivateAfterModeChange()
    {
        var windowHandle = WindowNative.GetWindowHandle(this);
        SetForegroundWindow(windowHandle);
        Activate();
    }

    private void PlaceAtBottomRight()
    {
        var display = GetCursorPos(out var cursor)
            ? DisplayArea.GetFromPoint(new PointInt32(cursor.X, cursor.Y), DisplayAreaFallback.Nearest)
            : DisplayArea.Primary;
        var workArea = display.WorkArea;
        var width = Math.Min(PopupWidth, workArea.Width);
        var height = Math.Min(PopupHeight, workArea.Height);
        var margin = Math.Min(EdgeMargin, Math.Min(workArea.Width - width, workArea.Height - height) / 2);
        var x = workArea.X + workArea.Width - width - margin;
        var y = workArea.Y + workArea.Height - height - margin;

        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr windowHandle);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
