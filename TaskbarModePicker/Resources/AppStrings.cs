using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;
using TaskbarModePicker.Models;

namespace TaskbarModePicker.Resources;

internal static class AppStrings
{
    private static readonly ResourceLoader Loader = new();

    public static string Get(string name) => Loader.GetString(name.Replace('.', '/'));

    public static string Format(string name, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(name), arguments);

    public static string GetModeName(TaskbarMode mode) => Get(mode switch
    {
        TaskbarMode.All => "ModeAll",
        TaskbarMode.MainAndWindow => "ModeMainAndWindow",
        TaskbarMode.Window => "ModeWindow",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, Get("UnknownTaskbarMode"))
    });
}
