using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using TaskbarModePicker.Models;
using TaskbarModePicker.Resources;
using TaskbarModePicker.Services;

namespace TaskbarModePicker.ViewModels;

public sealed class TaskbarModeViewModel : INotifyPropertyChanged
{
    private readonly ITaskbarModeService _taskbarModeService;
    private TaskbarMode? _currentMode;
    private bool _isApplying;
    private string? _statusMessage;

    public TaskbarModeViewModel(ITaskbarModeService taskbarModeService)
    {
        _taskbarModeService = taskbarModeService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TaskbarMode? CurrentMode
    {
        get => _currentMode;
        private set => SetProperty(ref _currentMode, value);
    }

    public bool IsApplying
    {
        get => _isApplying;
        private set
        {
            if (SetProperty(ref _isApplying, value))
            {
                OnPropertyChanged(nameof(CanInteract));
            }
        }
    }

    public bool CanInteract => !IsApplying;

    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(StatusVisibility));
            }
        }
    }

    public Visibility StatusVisibility => string.IsNullOrWhiteSpace(StatusMessage)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public void LoadCurrentMode()
    {
        try
        {
            CurrentMode = _taskbarModeService.ReadCurrentMode();
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            CurrentMode = null;
            StatusMessage = AppStrings.Format("Status.Error", ex.Message);
        }
    }

    public async Task ApplyModeAsync(TaskbarMode mode)
    {
        if (IsApplying)
        {
            return;
        }

        IsApplying = true;

        try
        {
            StatusMessage = AppStrings.Format("Status.Applying", AppStrings.GetModeName(mode));
            await _taskbarModeService.ApplyModeAsync(mode);
            CurrentMode = mode;
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            StatusMessage = AppStrings.Format("Status.Error", ex.Message);
        }
        finally
        {
            IsApplying = false;
        }
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
