using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
using TaskbarModePicker;
using TaskbarModePicker.Models;
using TaskbarModePicker.Services;
using TaskbarModePicker.ViewModels;
using Windows.System;
using Windows.UI.Core;

namespace TaskbarModePicker.Views;

public sealed partial class MainPage : Page
{
    private readonly (TaskbarMode Mode, Button Button, ProgressRing ProgressRing)[] _modeButtons;

    public MainPage()
    {
        InitializeComponent();

        ViewModel = new TaskbarModeViewModel(new TaskbarModeService());
        DataContext = ViewModel;
        _modeButtons =
        [
            (TaskbarMode.All, AllModeButton, AllModeProgressRing),
            (TaskbarMode.MainAndWindow, MainAndWindowModeButton, MainAndWindowModeProgressRing),
            (TaskbarMode.Window, WindowModeButton, WindowModeProgressRing)
        ];

        Loaded += OnPageLoaded;
    }

    public TaskbarModeViewModel ViewModel { get; }

    private void OnPageLoaded(object sender, RoutedEventArgs args)
    {
        ViewModel.LoadCurrentMode();
        UpdateSelectedMode();
        Focus(FocusState.Programmatic);
    }

    private async void ModeButton_Click(object sender, RoutedEventArgs args)
    {
        if (sender is not Button { Tag: string modeName } selectedButton ||
            !TaskbarModeMetadata.TryParse(modeName, out var mode))
        {
            return;
        }

        await ApplyModeAsync(mode, selectedButton);
    }

    private async Task ApplyModeAsync(TaskbarMode mode, Button selectedButton)
    {
        App.ActiveWindow?.SetModeChangeInProgress(true);
        SetApplyingMode(mode);

        try
        {
            await ViewModel.ApplyModeAsync(mode);
        }
        finally
        {
            var window = App.ActiveWindow;
            window?.ReactivateAfterModeChange();
            window?.SetModeChangeInProgress(false);
            SetApplyingMode(null);
            UpdateSelectedMode();
            selectedButton.Focus(FocusState.Programmatic);
        }
    }

    private void SetApplyingMode(TaskbarMode? applyingMode)
    {
        foreach (var (mode, button, progressRing) in _modeButtons)
        {
            button.Opacity = applyingMode is null
                ? 1.0
                : mode == applyingMode ? 0.85 : 0.45;
            progressRing.IsActive = mode == applyingMode;
            progressRing.Visibility = mode == applyingMode ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UpdateSelectedMode()
    {
        foreach (var (mode, button, _) in _modeButtons)
        {
            var styleKey = ViewModel.CurrentMode == mode
                ? "SelectedModeButtonStyle"
                : "ModeButtonStyle";
            button.Style = (Style)Resources[styleKey];
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs args)
    {
        App.ActiveWindow?.Close();
    }

    private async void MainPage_KeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (!ViewModel.IsApplying && args.Key is VirtualKey.Number1 or VirtualKey.Number2 or VirtualKey.Number3)
        {
            var modeIndex = (int)args.Key - (int)VirtualKey.Number1;
            args.Handled = true;
            await ApplyModeAsync(_modeButtons[modeIndex].Mode, _modeButtons[modeIndex].Button);
            return;
        }

        if (args.Key == VirtualKey.Escape && !ViewModel.IsApplying)
        {
            App.ActiveWindow?.Close();
            args.Handled = true;
            return;
        }

        if (ViewModel.IsApplying)
        {
            return;
        }

        var focusedElement = FocusManager.GetFocusedElement(XamlRoot);

        if (args.Key == VirtualKey.Tab)
        {
            var isShiftPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) &
                                  CoreVirtualKeyStates.Down) != 0;

            if (ReferenceEquals(focusedElement, CloseButton))
            {
                GetSelectedModeButton().Focus(FocusState.Keyboard);
            }
            else if (_modeButtons.Any(item => ReferenceEquals(item.Button, focusedElement)))
            {
                CloseButton.Focus(FocusState.Keyboard);
            }
            else if (ReferenceEquals(focusedElement, this))
            {
                (isShiftPressed ? CloseButton : GetSelectedModeButton()).Focus(FocusState.Keyboard);
            }
            else
            {
                return;
            }

            args.Handled = true;
            return;
        }

        if (args.Key is VirtualKey.Left or VirtualKey.Right)
        {
            var focusedModeIndex = Array.FindIndex(
                _modeButtons,
                item => ReferenceEquals(item.Button, focusedElement));

            if (focusedModeIndex >= 0)
            {
                var direction = args.Key == VirtualKey.Right ? 1 : -1;
                var nextIndex = (focusedModeIndex + direction + _modeButtons.Length) % _modeButtons.Length;
                _modeButtons[nextIndex].Button.Focus(FocusState.Keyboard);
                args.Handled = true;
            }
        }
    }

    private Button GetSelectedModeButton()
    {
        foreach (var (mode, button, _) in _modeButtons)
        {
            if (ViewModel.CurrentMode == mode)
            {
                return button;
            }
        }

        return AllModeButton;
    }
}
