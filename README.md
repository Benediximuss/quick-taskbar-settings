# Windows Taskbar Mode Shortcuts

A PowerShell tool for changing which taskbars show open apps on a multi-monitor Windows 11 setup.

## Goal
Switch among Windows' three multi-monitor taskbar app display modes from keyboard shortcuts, without restarting Explorer.

## Current status
`Taskbar-Universal-v22.ps1` is the confirmed working baseline. It changes the setting through Windows Settings using UI Automation, verifies the selection, closes Settings, and supports `All`, `MainAndWindow`, and `Window` modes.

## Run a mode

Double-click the matching launcher, or run it from Command Prompt:

- `Taskbar-All.cmd`
- `Taskbar-MainAndWindow.cmd`
- `Taskbar-Window.cmd`

Each launcher calls the corresponding mode in `Taskbar-Universal-v22.ps1` from its own folder.

## Next step
Assign keyboard shortcuts to these launchers in PowerToys Keyboard Manager.

Detailed engineering history, decisions, failure testing, and project continuity notes are kept locally in the Git-ignored `.codex` directory. The original foundational handoff is `.codex/TASKBAR_CONTEXT.md`.
