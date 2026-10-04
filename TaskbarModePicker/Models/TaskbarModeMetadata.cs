namespace TaskbarModePicker.Models;

public static class TaskbarModeMetadata
{
    public static bool TryParse(string? value, out TaskbarMode mode)
    {
        if (Enum.TryParse<TaskbarMode>(value, false, out mode) && Enum.IsDefined(mode))
        {
            return true;
        }

        mode = default;
        return false;
    }

    public static TaskbarMode? FromRegistryValue(object? value)
    {
        var numericValue = value switch
        {
            int intValue => intValue,
            long longValue when longValue >= int.MinValue && longValue <= int.MaxValue => (int)longValue,
            _ => (int?)null
        };

        if (numericValue is int number && Enum.IsDefined(typeof(TaskbarMode), number))
        {
            return (TaskbarMode)number;
        }

        return null;
    }
}
