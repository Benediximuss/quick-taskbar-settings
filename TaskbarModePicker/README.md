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

The app embeds `Taskbar-Universal-v23-Core.ps1` as a resource. When applying a mode, it writes the script into a unique temporary directory for hidden PowerShell to run, then removes that directory. The app does not rely on a loose script beside the executable or package.

## Publish a standalone x64 executable

The `win-x64-single-file` publish profile creates an unpackaged, self-contained x64 executable. The Windows App SDK and .NET dependencies are bundled into the executable and extracted to a temporary directory when it starts. The PowerShell core script is embedded in the executable and briefly written to a separate unique temporary directory while a mode is being applied.

From this directory, publish with:

```powershell
dotnet publish . -p:PublishProfile=win-x64-single-file
```

The output is `bin\Release\net10.0-windows10.0.26100.0\win-x64\publish-single-file\TaskbarModePicker.exe`. Copy that executable to a stable location before assigning its path in PowerToys Keyboard Manager. The first launch may take longer while dependencies are extracted.

Before relying on the published executable, launch it and test opening the picker, all three modes, and a failure message. This confirms both the WinUI runtime and bundled PowerShell script work from the self-contained publish.

## Test with package identity

Use the WinApp CLI to run the project with package identity. From the `TaskbarModePicker` directory, run:

```powershell
winapp run .
```

`winapp run` builds the project, registers its output as a loose-layout package, and launches it with package identity. Confirm the CLI reports the package identity and a process ID. Then select a mode: a successful change confirms the app can extract and invoke its embedded core script.

The `TaskbarModePicker (Package)` launch profile uses Visual Studio's `MsixPackage` profile type, which `dotnet run --launch-profile` does not support. Its unsupported-profile message does not by itself tell whether a separate WinApp CLI launch succeeded. Use the `winapp run` command above for an unambiguous package-identity test. This creates a development loose-layout package, not a signed, installed MSIX.

To generate an x64 MSIX for inspection without installing or launching it:

```powershell
dotnet build --no-restore -p:GenerateAppxPackageOnBuild=true -p:AppxPackageDir=artifacts\msix\ -p:AppxPackageSigningEnabled=false -p:Platform=x64 -p:RuntimeIdentifier=win-x64
```

The package is written under `artifacts\msix\`. This command disables signing, so use `winapp run .` for a runnable local test rather than attempting to install this inspection package directly.
