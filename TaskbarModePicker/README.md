# Taskbar Mode Picker

A small WinUI 3 flyout for selecting the Windows multi-monitor taskbar mode. The picker applies changes through the existing UI Automation core script; it does not write the registry to change the setting.

## Project layout

- `App.xaml` and `App.xaml.cs` start the application and register shared resources.
- `Views/` contains the popup window and its page. The page handles presentation and user input.
- `ViewModels/` holds selection, progress, and error state for the page.
- `Models/` defines the taskbar modes. Their numeric values match Explorer's `MMTaskbarMode` registry value.
- `Services/` reads the current registry value and invokes the PowerShell UI Automation core.
- `Resources/TaskbarModeIcons.xaml` contains the three vector icon templates used by the picker.
- `Assets/` contains the app and package artwork.

## Selection flow

1. The view model reads `MMTaskbarMode` to mark the saved mode when the picker opens.
2. Clicking a mode passes the enum value to `TaskbarModeService`.
3. The service starts hidden Windows PowerShell and calls `Invoke-TaskbarMode` from `Taskbar-Universal-v23-Core.ps1`.
4. On success, the view model updates the selected mode. On failure, it exposes the error in the flyout so the user can retry.

The project includes the core script at package path `Scripts/Taskbar-Universal-v23-Core.ps1` so the installed app can find it at runtime.

## Test with package identity

Use the WinApp CLI to run the project with package identity. From the `TaskbarModePicker` directory, run:

```powershell
winapp run .
```

`winapp run` builds the project, registers its output as a loose-layout package, and launches it with package identity. Confirm the CLI reports the package identity and a process ID. Then select a mode: a successful change confirms the app can find and invoke the bundled core script at `Scripts/Taskbar-Universal-v23-Core.ps1` relative to `AppContext.BaseDirectory`.

The `TaskbarModePicker (Package)` launch profile uses Visual Studio's `MsixPackage` profile type, which `dotnet run --launch-profile` does not support. Its unsupported-profile message does not by itself tell whether a separate WinApp CLI launch succeeded. Use the `winapp run` command above for an unambiguous package-identity test. This creates a development loose-layout package, not a signed, installed MSIX.

To generate an x64 MSIX for inspection without installing or launching it:

```powershell
dotnet build --no-restore -p:GenerateAppxPackageOnBuild=true -p:AppxPackageDir=artifacts\msix\ -p:AppxPackageSigningEnabled=false -p:Platform=x64 -p:RuntimeIdentifier=win-x64
```

The package is written under `artifacts\msix\`. This command disables signing, so use `winapp run .` for a runnable local test rather than attempting to install this inspection package directly.
