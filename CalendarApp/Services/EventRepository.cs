using System;
using System.Collections.Generic;
using System.Linq;
using CalendarApp.Models;

namespace CalendarApp.Services;

/// <summary>
/// In-memory store of every event, kept indexed by day so painting a month is a
/// dictionary lookup per cell rather than a scan of the whole list.
/// </summary>
public sealed class EventRepository
{
    private const int MaxSpanDays = 400;

    private static readonly IReadOnlyList<CalendarEvent> Empty = Array.Empty<CalendarEvent>();

    private readonly JsonFileStore<List<CalendarEvent>> _store;
    private readonly List<CalendarEvent> _events;
    private readonly Dictionary<DateTime, List<CalendarEvent>> _byDay = new();

    public EventRepository(JsonFileStore<List<CalendarEvent>> store)
    {
        _store = store;
        _events = store.Load() ?? new List<CalendarEvent>();
        Reindex();
    }

    /// <summary>Raised after any change that affects what a day should render.</summary>
    public event EventHandler? Changed;

    public IReadOnlyList<CalendarEvent> EventsOn(DateTime day) =>
        _byDay.TryGetValue(day.Date, out List<CalendarEvent>? list) ? list : Empty;

    public void Add(CalendarEvent calendarEvent)
    {
        _events.Add(calendarEvent);
        Commit();
    }

    public void Remove(CalendarEvent calendarEvent)
    {
        if (_events.Remove(calendarEvent))
        {
            Commit();
        }
    }

    /// <summary>Call after mutating an event that is already in the repository.</summary>
    public void Update(CalendarEvent calendarEvent)
    {
        if (!_events.Contains(calendarEvent))
        {
            _events.Add(calendarEvent);
        }

        Commit();
    }

    private void Commit()
    {
        Reindex();
        _store.Save(_events);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Reindex()
    {
        _byDay.Clear();

        foreach (CalendarEvent item in _events)
        {
            DateTime day = item.Start.Date;
            DateTime last = item.End.Date;

            // Guard against a bad end date turning into an unbounded loop.
            if (last < day)
            {
                last = day;
            }
            else if ((last - day).TotalDays > MaxSpanDays)
            {
                last = day.AddDays(MaxSpanDays);
            }

            while (day <= last)
            {
                if (!_byDay.TryGetValue(day, out List<CalendarEvent>? bucket))
                {
                    bucket = new List<CalendarEvent>();
                    _byDay[day] = bucket;
                }

                bucket.Add(item);
                day = day.AddDays(1);
            }
        }

        foreach (List<CalendarEvent> bucket in _byDay.Values)
        {
            bucket.Sort(static (a, b) => a.SortKey.CompareTo(b.SortKey));
        }
    }
}
