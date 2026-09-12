using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CalendarApp.Models;

namespace CalendarApp.Converters;

/// <summary>Shared brush cache so a colour is only ever parsed and allocated once.</summary>
internal static class BrushCache
{
    private static readonly Dictionary<string, SolidColorBrush> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static Brush For(string? hex)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return Brushes.Transparent;
        }

        if (Cache.TryGetValue(hex, out SolidColorBrush? cached))
        {
            return cached;
        }

        Brush brush;
        try
        {
            var solid = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
            solid.Freeze();
            Cache[hex] = solid;
            brush = solid;
        }
        catch (FormatException)
        {
            brush = Brushes.Transparent;
        }

        return brush;
    }
}

/// <summary>Turns an event colour such as <c>#2F6FED</c> into a brush.</summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        BrushCache.For(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Turns a palette key such as <c>Amber</c> into the brush that key stands for.</summary>
public sealed class ColorKeyToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        BrushCache.For(EventPalette.HexFor(value as string));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Collapses when the bound value is <c>true</c>.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Collapsed;
}
