using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CalendarApp.Common;
using CalendarApp.Models;

namespace CalendarApp.ViewModels;

/// <summary>
/// One cell of the month grid. Instances are created once and re-pointed at a new date as
/// the user pages through months, so navigation never rebuilds the visual tree.
/// </summary>
public sealed class DayCellViewModel : ObservableObject
{
    private DateTime _date;
    private bool _isCurrentMonth = true;
    private bool _isToday;
    private bool _isWeekend;
    private bool _isSelected;
    private int _hiddenEventCount;

    /// <summary>Events shown as chips inside the cell, trimmed to what the cell can fit.</summary>
    public ObservableCollection<CalendarEvent> VisibleEvents { get; } = new();

    public DateTime Date
    {
        get => _date;
        private set
        {
            if (SetProperty(ref _date, value))
            {
                OnPropertyChanged(nameof(DayNumber));
                OnPropertyChanged(nameof(AccessibleName));
            }
        }
    }

    public string DayNumber => _date.Day.ToString();

    /// <summary>False for the leading and trailing days borrowed from the neighbouring months.</summary>
    public bool IsCurrentMonth
    {
        get => _isCurrentMonth;
        private set => SetProperty(ref _isCurrentMonth, value);
    }

    public bool IsToday
    {
        get => _isToday;
        private set => SetProperty(ref _isToday, value);
    }

    public bool IsWeekend
    {
        get => _isWeekend;
        private set => SetProperty(ref _isWeekend, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public int HiddenEventCount
    {
        get => _hiddenEventCount;
        private set
        {
            if (SetProperty(ref _hiddenEventCount, value))
            {
                OnPropertyChanged(nameof(HasHiddenEvents));
                OnPropertyChanged(nameof(HiddenEventLabel));
            }
        }
    }

    public bool HasHiddenEvents => _hiddenEventCount > 0;

    public string HiddenEventLabel => $"+{_hiddenEventCount} more";

    public string AccessibleName => _date.ToString("D");

    /// <summary>Re-points this cell at <paramref name="date"/> and refreshes its chips.</summary>
    public void Update(DateTime date, bool isCurrentMonth, IReadOnlyList<CalendarEvent> events, int maxVisible)
    {
        Date = date;
        IsCurrentMonth = isCurrentMonth;
        IsToday = date == DateTime.Today;
        IsWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        int visibleCount = Math.Min(events.Count, Math.Max(maxVisible, 0));

        // Only touch the collection when the result actually differs; a no-op page render
        // would otherwise throw away and rebuild every chip container.
        if (!MatchesCurrent(events, visibleCount))
        {
            VisibleEvents.Clear();
            for (int i = 0; i < visibleCount; i++)
            {
                VisibleEvents.Add(events[i]);
            }
        }

        HiddenEventCount = events.Count - visibleCount;
    }

    private bool MatchesCurrent(IReadOnlyList<CalendarEvent> events, int visibleCount)
    {
        if (VisibleEvents.Count != visibleCount)
        {
            return false;
        }

        for (int i = 0; i < visibleCount; i++)
        {
            if (!ReferenceEquals(VisibleEvents[i], events[i]))
            {
                return false;
            }
        }

        return true;
    }
}
