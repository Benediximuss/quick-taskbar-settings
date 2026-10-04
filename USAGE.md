# User Guide

This guide covers the published picker, optional PowerToys shortcut setup, the script launchers in the repository, and common problems.

## Table of Contents

- [Download and first launch](#download-and-first-launch)
- [Choose a taskbar mode](#choose-a-taskbar-mode)
- [Open the picker with PowerToys](#open-the-picker-with-powertoys)
- [Use a script launcher](#use-a-script-launcher)
- [Troubleshooting](#troubleshooting)

## Download and first launch

### Requirements

- Windows 11 on an x64 PC for the published executable.
- Turkish-language Windows Settings. The current automation looks for Turkish setting and option names.
- PowerToys only if you want a keyboard shortcut.

### Steps

1. Download `TaskbarModePicker.exe` from the project's GitHub Releases page.
2. Save it in a stable location, for example a folder you use for portable utilities. You can move it later, but if PowerToys points to it, update the shortcut's app path after moving it.
3. Open the executable. It does not require a separate installer, .NET installation, or PowerShell script beside it.

The first launch may take longer while Windows extracts the bundled runtime files. Because the executable is unsigned, Windows may display a reputation warning. Only continue if you obtained it from the project's Releases page.

## Choose a taskbar mode

The picker displays three options and highlights the mode currently saved in Windows:

| Picker option | Windows value | Result |
| --- | ---: | --- |
| **Tüm görev çubukları** (All taskbars) | `0` | Open apps appear on every taskbar. |
| **Ana ve pencerenin açıldığı görev çubuğu** (Main and window taskbar) | `1` | Open apps appear on the main taskbar and the taskbar where each window is open. |
| **Pencerenin açıldığı görev çubuğu** (Taskbar where window is open) | `2` | An open app appears only on the taskbar where its window is open. |

Click an option or use the keyboard. The options can be navigated with the left and right arrow keys when the options row has focus. `Tab` moves between the options row and the close button. The top-row number keys `1`, `2`, and `3` (not the numpad keys) select the corresponding option. `Enter` and `Space` do not activate a mode. `Esc` closes the picker.

After you choose an option, the picker disables the choices and shows a progress indicator while it asks Windows Settings to apply the change. The saved selection updates after the app confirms success. If applying the option fails, the picker reports the error and remains available for another attempt.

## Open the picker with PowerToys

PowerToys Keyboard Manager can bind a shortcut to the picker:

1. Open **PowerToys Settings**.
2. Go to **Keyboard Manager → Remap a shortcut**.
3. Add a shortcut and choose **Start App** as the action.
4. Browse to `TaskbarModePicker.exe` and set it as the app to start.
5. Set **If running** to **Bring to front**. This lets the same shortcut raise the existing picker instead of opening another copy.
6. Save the remapping and try the shortcut.

If the picker occasionally appears behind the active app, set its **Low visibility** launch option to **Maximized** (Turkish interface: **Düşük Görünürlük → Ekranı kaplayan**). The picker remains a compact, non-resizable window; this option is a workaround for how the shortcut launches it.

## Use a script launcher

The repository's `.cmd` files can be assigned to shortcuts or run directly. Keep the `.cmd` and `.ps1` files together in the repository folder because the command files locate their scripts beside themselves.

| File | Action |
| --- | --- |
| `Taskbar-All.cmd` | Selects **All taskbars**. |
| `Taskbar-MainAndWindow.cmd` | Selects **Main and window taskbar**. |
| `Taskbar-Window.cmd` | Selects **Taskbar where window is open**. |
| `Taskbar-Toggle.cmd` | Toggles between **All taskbars** and **Taskbar where window is open**. A saved value of `1` also switches to `Window`. |

The direct mode launchers call `Taskbar-Universal-v22.ps1`. The toggle reads the current setting and calls the v23 core script to apply its chosen mode. The scripts change the setting using the Windows Settings UI; they do not write `MMTaskbarMode` to apply a change.

Running the `.ps1` file from an already open PowerShell session keeps output visible in that session. When a `.cmd` file is double-clicked or launched by a shortcut, its console may close after a successful run. On failures, the launchers are designed to keep the console open so the error can be read.

## Troubleshooting

### The picker reports that it could not find a setting or option

The UI Automation script currently searches for Turkish names in Windows Settings. Confirm that Windows display language is Turkish and that Taskbar Settings still uses the expected labels. If Settings has changed, the app reports a failure rather than selecting an unrelated control.

### The picker opens behind another window

Set the PowerToys launch visibility to **Maximized** (Turkish: **Ekranı kaplayan**) and set **If running** to **Bring to front**. If it is already running, pressing its shortcut again should raise it.

### The picker shows an error while applying a mode

Keep the error message for troubleshooting. The app waits a limited time for the Settings window and controls, and verifies the selection before it reports success. Check that Windows Settings can open normally and that no dialog is blocking its taskbar page, then try again.

### The script console closes too quickly

For output in a persistent terminal, open PowerShell in the repository folder and run the desired `.ps1` or `.cmd` from there. A failed `.cmd` launch should pause and display its error; successful shortcut launches may close the console automatically.

### The shortcut stops working after moving the executable

Edit the PowerToys remapping and select the executable at its new location. PowerToys stores the app path used when the shortcut was configured.

## Related documentation

- [README.md](README.md) — project overview, modes, requirements, and quick setup.
- [ARCHITECTURE.md](ARCHITECTURE.md) — application design, scripts, and publishing details.
