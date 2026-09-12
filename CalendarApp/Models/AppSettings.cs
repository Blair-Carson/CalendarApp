using System;

namespace CalendarApp.Models;

public enum AppTheme
{
    Light,
    Dark,
}

/// <summary>User preferences persisted between runs.</summary>
public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Light;

    public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Sunday;

    public bool ShowWeekNumbers { get; set; }

    public double WindowWidth { get; set; } = 1180;

    public double WindowHeight { get; set; } = 760;
}
