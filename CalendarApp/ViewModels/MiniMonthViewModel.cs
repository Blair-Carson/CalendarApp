using System;
using System.Collections.Generic;
using CalendarApp.Common;

namespace CalendarApp.ViewModels;

/// <summary>A single day inside a year-view thumbnail.</summary>
public sealed class MiniDayViewModel : ObservableObject
{
    private string _dayNumber = string.Empty;
    private bool _isToday;
    private bool _hasEvents;
    private bool _isWeekend;

    /// <summary>Empty for padding cells outside the month.</summary>
    public string DayNumber
    {
        get => _dayNumber;
        private set => SetProperty(ref _dayNumber, value);
    }

    public bool IsToday
    {
        get => _isToday;
        private set => SetProperty(ref _isToday, value);
    }

    public bool HasEvents
    {
        get => _hasEvents;
        private set => SetProperty(ref _hasEvents, value);
    }

    public bool IsWeekend
    {
        get => _isWeekend;
        private set => SetProperty(ref _isWeekend, value);
    }

    public void SetDay(DateTime date, bool hasEvents)
    {
        DayNumber = date.Day.ToString();
        IsToday = date == DateTime.Today;
        IsWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        HasEvents = hasEvents;
    }

    public void Clear()
    {
        DayNumber = string.Empty;
        IsToday = false;
        IsWeekend = false;
        HasEvents = false;
    }
}

/// <summary>One month thumbnail in the year view.</summary>
public sealed class MiniMonthViewModel : ObservableObject
{
    /// <summary>Six weeks of seven days covers every possible month layout.</summary>
    public const int CellCount = 42;

    private string _name = string.Empty;
    private bool _isCurrentMonth;

    public MiniMonthViewModel(int month)
    {
        Month = month;

        var days = new List<MiniDayViewModel>(CellCount);
        for (int i = 0; i < CellCount; i++)
        {
            days.Add(new MiniDayViewModel());
        }

        Days = days;
    }

    public int Month { get; }

    public IReadOnlyList<MiniDayViewModel> Days { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>True when this thumbnail is the month the real today falls in.</summary>
    public bool IsCurrentMonth
    {
        get => _isCurrentMonth;
        set => SetProperty(ref _isCurrentMonth, value);
    }
}
