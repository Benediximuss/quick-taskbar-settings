# Windows Taskbar Mode Picker

A small Windows 11 utility for quickly choosing which taskbars show your open apps. It replaces the usual trip through Taskbar Settings with a compact picker that can also be opened from a PowerToys keyboard shortcut.

## Overview

Windows 11 has a setting for choosing where open app buttons appear when more than one display is connected. The setting is a few steps into the Taskbar Settings page, so changing it repeatedly can be tedious. This project provides a small flyout with the three available modes and highlights the mode currently saved in Windows.

Choosing a mode opens the relevant Windows Settings page, selects the requested option, verifies that Windows accepted it, and closes Settings. The app reads the saved mode from the current user's registry to display the selection, but applies changes through the Settings interface. It does not write the setting directly to the registry or restart Explorer.

## Modes

| Mode | What it does |
| --- | --- |
| **All taskbars** (`All`, value `0`) | Shows open apps on every taskbar. |
| **Main and window taskbar** (`MainAndWindow`, value `1`) | Shows open apps on the main taskbar and the taskbar where each window is open. |
| **Taskbar where window is open** (`Window`, value `2`) | Shows an open app only on the taskbar where its window is open. |

The names and numeric values match Windows' `MMTaskbarMode` setting.

## Download and use

For everyday use, download `TaskbarModePicker.exe` from the project's GitHub Releases page. The standalone x64 executable includes the .NET and Windows App SDK runtime dependencies and the PowerShell script used by the app. No installer or separate script file is needed.

1. Save the executable somewhere convenient, such as a personal tools folder.
2. Open `TaskbarModePicker.exe`.
3. Choose a mode. The currently saved mode is highlighted, and the picker shows progress while Windows applies a change.

The app is intended for Windows 11 x64 with Turkish-language Windows Settings. The current automation locates the setting and choices by their Turkish UI names; on another Settings display language, it may report that it could not find the controls.

The executable is unsigned, so Windows may show a reputation warning the first time it is downloaded. The first launch can also take longer while bundled runtime files are extracted. Use the executable from the project's Releases page.

## PowerToys keyboard shortcut

PowerToys is optional. To open the picker with a shortcut:

1. Open **PowerToys Settings → Keyboard Manager → Remap a shortcut**.
2. Add the shortcut you want and choose **Start App** for its action.
3. Set **App** to the full path of `TaskbarModePicker.exe`.
4. Set **If running** to **Bring to front**, so pressing the shortcut again raises the existing picker.

If the picker sometimes opens behind another window, set its launch visibility to **Maximized** (Turkish: **Ekranı kaplayan**). This affects how the app is launched; the picker itself remains compact and non-resizable.

## Script launchers

The repository also includes command files for people who prefer direct shortcuts to a specific mode or to a two-mode toggle:

- `Taskbar-All.cmd` selects **All taskbars**.
- `Taskbar-MainAndWindow.cmd` selects **Main and window taskbar**.
- `Taskbar-Window.cmd` selects **Taskbar where window is open**.
- `Taskbar-Toggle.cmd` switches between **All taskbars** and **Taskbar where window is open**. If the saved mode is `MainAndWindow`, it switches to `Window`.

These launchers use the scripts in the repository and are separate from the standalone picker executable. For practical setup and troubleshooting, see [USAGE.md](USAGE.md). For implementation details and publishing instructions, see [ARCHITECTURE.md](ARCHITECTURE.md).

## Requirements

- Windows 11, x64 for the published picker executable.
- Turkish-language Windows Settings for the current UI Automation labels.
- PowerToys only if you want to open the picker with a global keyboard shortcut.
- Windows PowerShell is included with Windows and is used internally by the picker and scripts.

## Project structure

The repository contains the WinUI 3 picker, its embedded PowerShell automation script, and standalone `.cmd`/`.ps1` launchers. The app's Turkish interface text is in `TaskbarModePicker/Strings/tr-TR/Resources.resw`; the UI and application logic are organized under `Views`, `ViewModels`, `Models`, `Services`, and `Resources`.

## Documentation

- [USAGE.md](USAGE.md) — downloading, opening, setting up a PowerToys shortcut, using the script launchers, and troubleshooting.
- [ARCHITECTURE.md](ARCHITECTURE.md) — project structure, app and script behavior, failure handling, and build/publish workflow.
