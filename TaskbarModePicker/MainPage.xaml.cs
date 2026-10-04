using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace TaskbarModePicker;

public sealed partial class MainPage : Page
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string RegistryValueName = "MMTaskbarMode";
    private const string CoreFileName = "Taskbar-Universal-v23-Core.ps1";

    private readonly (string Mode, Button Button)[] _modeCards;
    private string? _currentMode;

    public MainPage()
    {
        InitializeComponent();

        _modeCards =
        [
            ("All", AllModeButton),
            ("MainAndWindow", MainAndWindowModeButton),
            ("Window", WindowModeButton)
        ];

        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs args)
    {
        _currentMode = ReadCurrentMode();
        UpdateSelectedMode(_currentMode);

        AllModeButton.Focus(FocusState.Programmatic);
    }

    private async void ModeButton_Click(object sender, RoutedEventArgs args)
    {
        if (sender is not Button { Tag: string mode } selectedButton || GetModeLabel(mode) is null)
        {
            return;
        }

        SetInputEnabled(false);
        ToolTipService.SetToolTip(StatusText, null);
        StatusText.Text = $"'{GetModeLabel(mode)}' ayarı uygulanıyor…";
        StatusText.Visibility = Visibility.Visible;
        App.ActiveWindow?.SetModeChangeInProgress(true);

        try
        {
            await InvokeTaskbarCoreAsync(mode);
            _currentMode = mode;
            UpdateSelectedMode(mode);
            App.ActiveWindow?.SetModeChangeInProgress(false);
            SetInputEnabled(true);
            StatusText.Visibility = Visibility.Collapsed;
            selectedButton.Focus(FocusState.Programmatic);
        }
        catch (Exception ex)
        {
            App.ActiveWindow?.SetModeChangeInProgress(false);
            SetInputEnabled(true);
            StatusText.Text = $"HATA · {ex.Message}";
            ToolTipService.SetToolTip(StatusText, ex.Message);
            selectedButton.Focus(FocusState.Programmatic);
        }
    }

    private static string? ReadCurrentMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: false);
            var value = key?.GetValue(RegistryValueName);
            var mode = value switch
            {
                int intMode => intMode,
                long longMode when longMode >= int.MinValue && longMode <= int.MaxValue => (int)longMode,
                _ => -1
            };

            return mode switch
            {
                0 => "All",
                1 => "MainAndWindow",
                2 => "Window",
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? GetModeLabel(string mode) => mode switch
    {
        "All" => "Tüm görev çubukları",
        "MainAndWindow" => "Ana görev çubuğu ve pencerenin açıldığı görev çubuğu",
        "Window" => "Pencerenin açıldığı görev çubuğu",
        _ => null
    };

    private void UpdateSelectedMode(string? mode)
    {
        foreach (var (cardMode, button) in _modeCards)
        {
            var selected = cardMode == mode;
            button.Style = (Style)Resources[selected ? "SelectedModeButtonStyle" : "ModeButtonStyle"];
        }
    }

    private void SetInputEnabled(bool enabled)
    {
        foreach (var (_, button) in _modeCards)
        {
            button.IsEnabled = enabled;
        }

        PopupCloseButton.IsEnabled = enabled;
    }

    private static async Task InvokeTaskbarCoreAsync(string mode)
    {
        var corePath = Path.Combine(AppContext.BaseDirectory, "Scripts", CoreFileName);
        if (!File.Exists(corePath))
        {
            throw new FileNotFoundException(
                "Görev çubuğu ayar işlevi uygulama paketinde bulunamadı.",
                corePath);
        }

        var escapedPath = corePath.Replace("'", "''", StringComparison.Ordinal);
        var command = "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false); " +
                      "$ErrorActionPreference = 'Stop'; " +
                      $". '{escapedPath}'; " +
                      "try { " +
                      $"Invoke-TaskbarMode -Mode '{mode}'; " +
                      "exit 0 " +
                      "} catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }";

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-STA");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command);

        using var process = new Process { StartInfo = startInfo };
        bool started;
        try
        {
            started = process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("PowerShell başlatılamadı. Görev çubuğu ayarı değiştirilmedi.", ex);
        }

        if (!started)
        {
            throw new InvalidOperationException("PowerShell işlemi başlatılamadı. Görev çubuğu ayarı değiştirilmedi.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(stdoutTask, stderrTask);

        if (process.ExitCode != 0)
        {
            var detail = stderrTask.Result.Trim();
            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = stdoutTask.Result.Trim();
            }

            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = $"PowerShell işlemi {process.ExitCode} koduyla sonlandı.";
            }

            throw new InvalidOperationException(detail);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs args)
    {
        App.ActiveWindow?.Close();
    }

    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.Escape && App.ActiveWindow?.IsModeChangeInProgress != true)
        {
            App.ActiveWindow?.Close();
            args.Handled = true;
        }
    }
}
