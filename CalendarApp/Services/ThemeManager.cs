using System;
using System.Windows;
using CalendarApp.Models;

namespace CalendarApp.Services;

/// <summary>
/// Swaps the palette dictionary in place. Only colour resources live in the palette, so
/// switching themes is a single dictionary replacement and every DynamicResource updates
/// itself — no part of the visual tree has to be rebuilt.
/// </summary>
public static class ThemeManager
{
    /// <summary>Index of the palette inside <c>App.xaml</c>'s merged dictionaries.</summary>
    private const int PaletteSlot = 0;

    /// <summary>Raised after the palette has been swapped.</summary>
    public static event EventHandler? ThemeChanged;

    public static AppTheme Current { get; private set; } = AppTheme.Light;

    public static void Apply(AppTheme theme)
    {
        var palette = new ResourceDictionary
        {
            Source = new Uri(
                theme == AppTheme.Dark ? "Themes/Palette.Dark.xaml" : "Themes/Palette.Light.xaml",
                UriKind.Relative),
        };

        Application.Current.Resources.MergedDictionaries[PaletteSlot] = palette;
        Current = theme;
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static AppTheme Toggle()
    {
        Apply(Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
        return Current;
    }
}
