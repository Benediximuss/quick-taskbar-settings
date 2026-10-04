# Architecture and Technical Documentation

This document describes the taskbar mode scripts, the WinUI 3 picker, how a mode change flows through the application, and how to build the standalone executable.

## Table of Contents

- [Project Overview](#project-overview)
- [Repository Structure](#repository-structure)
- [System Architecture](#system-architecture)
- [Mode Representation](#mode-representation)
- [Picker Application](#picker-application)
- [PowerShell Scripts](#powershell-scripts)
- [Mode Change Flow](#mode-change-flow)
- [Failure Handling](#failure-handling)
- [Build and Run](#build-and-run)
- [Publish the Standalone Executable](#publish-the-standalone-executable)
- [Release Workflow](#release-workflow)

## Project Overview

The repository provides two ways to change the Windows taskbar mode:

1. A compact WinUI 3 application, `TaskbarModePicker`, presents the three available choices and shows the mode currently saved by Windows.
2. PowerShell scripts and `.cmd` wrappers apply a chosen mode directly, or toggle between the two commonly used modes.

The picker reads `MMTaskbarMode` from the current user's Explorer registry settings to display the saved value. To apply a change, both the app and scripts use UI Automation against the Windows Settings page. They do not write the registry value to make the change and do not restart Explorer.

## Repository Structure

```text
.
├── Taskbar-All.cmd                    # Apply All through the v22 script
├── Taskbar-MainAndWindow.cmd          # Apply MainAndWindow through v22
├── Taskbar-Window.cmd                 # Apply Window through v22
├── Taskbar-Toggle.cmd                 # Console wrapper for the two-mode toggle
├── Taskbar-Toggle.ps1                 # Read current mode and choose next mode
├── Taskbar-Universal-v22.ps1          # Standalone parameterized UI Automation script
├── Taskbar-Universal-v23-Core.ps1     # Reusable Invoke-TaskbarMode function
├── probes/                            # Ignored local timing/failure investigation files
├── README.md                          # Project introduction and quick setup
├── USAGE.md                           # User instructions and troubleshooting
├── ARCHITECTURE.md                    # This technical document
└── TaskbarModePicker/
    ├── App.xaml(.cs)                  # App resources and startup
    ├── Models/                        # Mode enum and registry value conversion
    ├── Resources/                     # Localized-string access and vector icons
    ├── Services/                      # Registry read and PowerShell invocation
    ├── Strings/tr-TR/Resources.resw   # Turkish user-facing strings
    ├── ViewModels/                    # Selection, progress, and status state
    ├── Views/                         # Picker page and popup window
    └── Properties/PublishProfiles/    # Standalone publish configuration
```

## System Architecture

```text
Picker path
───────────
WinUI view → view model → TaskbarModeService
                             ├─ read MMTaskbarMode (display current choice)
                             └─ extract embedded v23 core → Windows PowerShell
                                                             └─ UI Automation → Settings

Direct script path
──────────────────
Mode .cmd → v22 script ──────────────────────────────┐
Toggle .cmd → Toggle.ps1 → read MMTaskbarMode → v23 core
                                                    └─ UI Automation → Settings
```

The WinUI project is organized by responsibility: `Views` describe the window and controls, `ViewModels` hold presentation state and actions, `Models` define the mode values, `Services` perform operating-system interaction, and `Resources` provide reusable UI resources and localized text access. The mode choice behavior lives in the view model, while the service contains the registry read and process invocation details.

## Mode Representation

`TaskbarMode` is an enum whose values intentionally match Windows' `MMTaskbarMode` registry value:

| Enum member | Value | Picker meaning |
| --- | ---: | --- |
| `All` | `0` | Show apps on all taskbars. |
| `MainAndWindow` | `1` | Show apps on the main and window taskbars. |
| `Window` | `2` | Show apps only on the taskbar where each window is open. |

`TaskbarModeMetadata` converts the registry value to a known enum member. An absent or unrecognized value does not silently map to another option; the picker can show that it could not determine the saved mode.

## Picker Application

### Views

- `Views/MainWindow.xaml` and its code-behind define the compact, non-resizable picker window and its placement near the lower-right of the display.
- `Views/MainPage.xaml` defines the row of three equally sized mode buttons and their visual states. `MainPage.xaml.cs` handles keyboard and pointer input, including option navigation and selection.

The picker supports arrow-key navigation within the options row, `Tab` movement between the row and the close button, and number-key selection (`1`, `2`, and `3`). `Enter` and `Space` are intentionally not mode-selection shortcuts; `Esc` dismisses the picker.

### View model

`ViewModels/TaskbarModeViewModel` reads and exposes the current selection, tracks whether a change is in progress, and provides the localized status or error shown by the view. It updates the selected mode only after the service reports a successful application. While a request is in progress, it disables the choices and identifies the requested mode.

### Service

`Services/TaskbarModeService` implements the operating-system work:

1. Reads `MMTaskbarMode` from `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced` with read-only access.
2. Loads `Taskbar-Universal-v23-Core.ps1` from the app's embedded resources.
3. Writes the script to a uniquely named directory below the user's temporary directory.
4. Starts Windows PowerShell without showing a separate console, invokes `Invoke-TaskbarMode`, and waits for it to exit.
5. Converts a nonzero process exit into an application error, then attempts to remove the temporary script directory.

The PowerShell file is embedded at build time, so the published executable does not need a companion `.ps1` file. The app uses Windows PowerShell from Windows at runtime.

### UI resources and localization

- `Strings/tr-TR/Resources.resw` contains the picker’s Turkish user-facing text.
- `Resources/AppStrings.cs` provides a small resource lookup and formatting helper.
- `Resources/TaskbarModeIcons.xaml` contains the vector icon templates used by the mode buttons.

The app's UI strings are resources; the UI Automation script separately uses Turkish names exposed by Windows Settings. Supporting another Windows display language requires updating and validating those automation names as well as providing localized UI resources.

## PowerShell Scripts

### `Taskbar-Universal-v22.ps1`

The standalone baseline accepts one of `All`, `MainAndWindow`, or `Window`. It opens Taskbar Settings, locates the target combo box and option through UI Automation, selects the option, verifies selection, and closes the Settings window. `Taskbar-All.cmd`, `Taskbar-MainAndWindow.cmd`, and `Taskbar-Window.cmd` pass a fixed mode to this script.

### `Taskbar-Universal-v23-Core.ps1`

This script defines `Invoke-TaskbarMode`, which provides the same Settings UI Automation operation as a callable function. The picker embeds and invokes this core in its own PowerShell process. The toggle script also calls the core after choosing its target mode. The v22 script remains available as the separate standalone launcher path.

The core uses bounded waits for the Settings window and controls, selects the requested option, and verifies it. Its optional probe parameter writes timing diagnostics and is intended for local investigation.

### `Taskbar-Toggle.ps1` and `.cmd` wrappers

The toggle reads `MMTaskbarMode` and chooses the other common mode:

| Current value | Next value | Reason |
| ---: | ---: | --- |
| `0` (`All`) | `2` (`Window`) | Switch between the two common modes. |
| `1` (`MainAndWindow`) | `2` (`Window`) | Treat the less-used mode as a fallback to `Window`. |
| `2` (`Window`) | `0` (`All`) | Switch between the two common modes. |

The registry read decides the next mode; applying that mode still goes through Settings UI Automation. `Taskbar-Toggle.cmd` is the shortcut-friendly console wrapper.

## Mode Change Flow

```text
User picks an option
        ↓
ViewModel marks the request in progress and disables the choices
        ↓
TaskbarModeService extracts the embedded core script
        ↓
Windows PowerShell invokes Invoke-TaskbarMode -Mode <name>
        ↓
Core opens Settings, locates and selects the option, then verifies it
        ↓
PowerShell exits; the app removes its temporary script directory
        ↓
ViewModel updates the saved selection or shows an error
```

Opening the picker is read-only with respect to the taskbar mode. The mode changes only after the user selects an option and the Settings automation succeeds.

## Failure Handling

- If the registry value cannot be read, the app reports a localized error rather than guessing the selected mode.
- If the Settings page or target controls cannot be found within the script's wait limit, the script fails with a descriptive message.
- A selection is not reported as successful until UI Automation confirms it.
- The app captures PowerShell output and errors and reports a failed exit to the view model.
- Cleanup of the temporary script folder is best-effort. Windows reclaims any file that could not be removed at that time.
- The script launchers keep a console available on errors so the message can be read.

The UI Automation targets Turkish Settings names. A Windows update that changes the control labels or a different display language can prevent a match. In that case the scripts stop rather than clicking a similarly positioned control.

## Build and Run

### Requirements

- Windows 11.
- .NET 10 SDK.
- WinApp CLI for the package-identity development launch.

From the `TaskbarModePicker` directory, build and launch the development app with:

```powershell
winapp run .
```

The `TaskbarModePicker (Package)` launch profile in `Properties/launchSettings.json` is an MSIX profile and is not supported by `dotnet run --launch-profile`. Use `winapp run .` for the development launch.

## Publish the Standalone Executable

The tracked profile `TaskbarModePicker/Properties/PublishProfiles/win-x64-single-file.pubxml` publishes an unpackaged, self-contained x64 executable. It bundles the .NET and Windows App SDK runtime dependencies, plus the embedded PowerShell core. Windows PowerShell itself is supplied by Windows. The bundled runtime files are extracted under the user's temporary directory at startup, so the distribution is a single executable but not a zero-extraction application.

From the `TaskbarModePicker` directory, run:

```powershell
dotnet publish . -p:PublishProfile=win-x64-single-file
```

The output is:

```text
bin/Release/net10.0-windows10.0.26100.0/win-x64/publish-single-file/TaskbarModePicker.exe
```

Run the publish command again after source changes to update the output. Before attaching the executable to a public release, launch it from the publish output and confirm that it opens and can apply a mode. The profile targets x64 and does not code-sign the executable.

## Release Workflow

The release tag for the first public version is `v1.0.0`. To publish it after pushing the repository to GitHub:

1. Create a public, empty GitHub repository. Do not initialize it with a README, `.gitignore`, or license; this project already has its own files.
2. From the local repository root, add the repository as `origin` and push the branch and release tag:

   ```powershell
   git remote add origin https://github.com/<account>/<repository>.git
   git push -u origin HEAD:main
   git push origin v1.0.0
   ```

   If `origin` already exists, inspect it before changing the URL.
3. On GitHub, open **Releases → Draft a new release**, choose the `v1.0.0` tag, and attach `TaskbarModePicker.exe` from the publish output directory.
4. Publish the release. The attached executable is the direct download for people who do not want to build the project.

Do not commit generated `bin/` or `obj/` output. The public executable is unsigned, so Windows may show a reputation warning to people downloading it.
