namespace TaskbarModePicker.Models;

/// <summary>
/// The numeric values intentionally match Explorer's MMTaskbarMode registry value.
/// </summary>
public enum TaskbarMode
{
    All = 0,
    MainAndWindow = 1,
    Window = 2
}
