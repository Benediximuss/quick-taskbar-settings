using TaskbarModePicker.Models;

namespace TaskbarModePicker.Services;

public interface ITaskbarModeService
{
    TaskbarMode? ReadCurrentMode();

    Task ApplyModeAsync(TaskbarMode mode);
}
