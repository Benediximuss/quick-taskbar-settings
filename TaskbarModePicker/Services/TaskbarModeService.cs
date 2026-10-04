using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using TaskbarModePicker.Models;
using TaskbarModePicker.Resources;

namespace TaskbarModePicker.Services;

public sealed class TaskbarModeService : ITaskbarModeService
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string RegistryValueName = "MMTaskbarMode";
    private const string CoreFileName = "Taskbar-Universal-v23-Core.ps1";

    public TaskbarMode? ReadCurrentMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: false);
            return TaskbarModeMetadata.FromRegistryValue(key?.GetValue(RegistryValueName));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                AppStrings.Format("Error.RegistryRead", RegistryValueName, ex.Message),
                ex);
        }
    }

    public async Task ApplyModeAsync(TaskbarMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, AppStrings.Get("Error.InvalidMode"));
        }

        var corePath = Path.Combine(AppContext.BaseDirectory, "Scripts", CoreFileName);
        if (!File.Exists(corePath))
        {
            throw new FileNotFoundException(
                AppStrings.Get("Error.CoreMissing"),
                corePath);
        }

        var escapedPath = corePath.Replace("'", "''", StringComparison.Ordinal);
        var scriptMode = mode.ToString();
        var command = "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false); " +
                      "$ErrorActionPreference = 'Stop'; " +
                      $". '{escapedPath}'; " +
                      "try { " +
                      $"Invoke-TaskbarMode -Mode '{scriptMode}' -ForegroundProcessId {Environment.ProcessId}; " +
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
            throw new InvalidOperationException(AppStrings.Get("Error.PowerShellStart"), ex);
        }

        if (!started)
        {
            throw new InvalidOperationException(AppStrings.Get("Error.PowerShellProcessStart"));
        }

        // The picker is still foreground immediately after starting PowerShell.
        // Grant that child process the ability to transfer foreground permission
        // back to the picker after it closes Windows Settings.
        AllowSetForegroundWindow((uint)process.Id);

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
                detail = AppStrings.Format("Error.PowerShellExitCode", process.ExitCode);
            }

            throw new InvalidOperationException(detail);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(uint processId);
}
