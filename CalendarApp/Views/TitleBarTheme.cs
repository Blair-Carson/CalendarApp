using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CalendarApp.Models;
using CalendarApp.Services;

namespace CalendarApp.Views;

/// <summary>
/// Keeps the OS title bar in step with the in-app theme, so a dark window does not sit
/// under a light caption. Silently does nothing on Windows builds that predate the
/// immersive dark mode attribute.
/// </summary>
internal static class TitleBarTheme
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>Applies the current theme to <paramref name="window"/> and follows later changes.</summary>
    public static void Attach(Window window)
    {
        void Apply(object? sender, EventArgs e) => ApplyTo(window, ThemeManager.Current);

        if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
        {
            Apply(null, EventArgs.Empty);
        }
        else
        {
            window.SourceInitialized += Apply;
        }

        ThemeManager.ThemeChanged += Apply;
        window.Closed += (_, _) => ThemeManager.ThemeChanged -= Apply;
    }

    private static void ApplyTo(Window window, AppTheme theme)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        int useDark = theme == AppTheme.Dark ? 1 : 0;

        try
        {
            DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // Not available on this OS; the caption keeps its default colours.
        }
    }
}
