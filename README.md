# Windows Taskbar Mode Shortcuts

A PowerShell tool for changing which taskbars show open apps on a multi-monitor Windows 11 setup.

## Goal
Switch among Windows' three multi-monitor taskbar app display modes from keyboard shortcuts, without restarting Explorer.

## Current status
`Taskbar-Universal-v22.ps1` is the confirmed working baseline. It changes the setting through Windows Settings using UI Automation, verifies the selection, closes Settings, and supports `All`, `MainAndWindow`, and `Window` modes. `Taskbar-Toggle.cmd` provides the common two-mode workflow while leaving v22 unchanged.

## Run a mode

Double-click the matching launcher, or run it from Command Prompt:

- `Taskbar-All.cmd`
- `Taskbar-MainAndWindow.cmd`
- `Taskbar-Window.cmd`

Each launcher calls the corresponding mode in `Taskbar-Universal-v22.ps1` from its own folder.

## Toggle between the two common modes

Run `Taskbar-Toggle.cmd` to switch between `All` and `Window`. It reads the current `MMTaskbarMode` registry value, whose existing values are `0 = All`, `1 = MainAndWindow`, and `2 = Window`. Values `0` and `1` select `Window`; value `2` selects `All`. It applies and verifies the change through v22's Settings UI automation without writing the registry directly. Error messages are in Turkish, and the launcher keeps its console open after a failure so the details remain visible.

## Keyboard shortcuts
Assign `Taskbar-Toggle.cmd` to a PowerToys Keyboard Manager shortcut for the common two-mode workflow. The individual mode launchers remain available when you need to select a specific mode.

Detailed engineering history, decisions, failure testing, and project continuity notes are kept locally in the Git-ignored `.codex` directory. The original foundational handoff is `.codex/TASKBAR_CONTEXT.md`.
