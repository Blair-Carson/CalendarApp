using System;
using System.Text.Json.Serialization;
using CalendarApp.Common;

namespace CalendarApp.Models;

/// <summary>A single appointment. May span more than one day.</summary>
public sealed class CalendarEvent : ObservableObject
{
    private string _title = string.Empty;
    private DateTime _start = DateTime.Today;
    private DateTime _end = DateTime.Today;
    private bool _isAllDay;
    private string _notes = string.Empty;
    private string _colorKey = EventPalette.DefaultKey;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>Inclusive start. For an all-day event the time component is ignored.</summary>
    public DateTime Start
    {
        get => _start;
        set
        {
            if (SetProperty(ref _start, value))
            {
                OnPropertyChanged(nameof(RangeLabel));
            }
        }
    }

    /// <summary>Inclusive end. For an all-day event the time component is ignored.</summary>
    public DateTime End
    {
        get => _end;
        set
        {
            if (SetProperty(ref _end, value))
            {
                OnPropertyChanged(nameof(RangeLabel));
            }
        }
    }

    public bool IsAllDay
    {
        get => _isAllDay;
        set
        {
            if (SetProperty(ref _isAllDay, value))
            {
                OnPropertyChanged(nameof(RangeLabel));
            }
        }
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    /// <summary>Key into <see cref="EventPalette"/>. Stored by name so the palette can be retuned later.</summary>
    public string ColorKey
    {
        get => _colorKey;
        set
        {
            if (SetProperty(ref _colorKey, value))
            {
                OnPropertyChanged(nameof(ColorHex));
            }
        }
    }

    [JsonIgnore]
    public string ColorHex => EventPalette.HexFor(_colorKey);

    [JsonIgnore]
    public bool HasNotes => !string.IsNullOrWhiteSpace(_notes);

    /// <summary>Full label used in the agenda, e.g. "09:30 - 10:15".</summary>
    [JsonIgnore]
    public string RangeLabel
    {
        get
        {
            if (_isAllDay)
            {
                return SpansMultipleDays
                    ? $"All day · {_start:ddd d MMM} – {_end:ddd d MMM}"
                    : "All day";
            }

            return SpansMultipleDays
                ? $"{_start:ddd d MMM HH:mm} – {_end:ddd d MMM HH:mm}"
                : $"{_start:t} – {_end:t}";
        }
    }

    [JsonIgnore]
    public bool SpansMultipleDays => _start.Date != _end.Date;

    /// <summary>True when this event covers <paramref name="day"/>.</summary>
    public bool OccursOn(DateTime day)
    {
        DateTime date = day.Date;
        return date >= _start.Date && date <= _end.Date;
    }

    /// <summary>Ordering key within a day: all-day events float to the top, then by start time.</summary>
    public (int Bucket, DateTime Start, string Title) SortKey => (_isAllDay ? 0 : 1, _start, _title);

    public CalendarEvent Clone() => new()
    {
        Id = Id,
        Title = _title,
        Start = _start,
        End = _end,
        IsAllDay = _isAllDay,
        Notes = _notes,
        ColorKey = _colorKey,
    };

    /// <summary>Copies every editable field from <paramref name="other"/> onto this instance.</summary>
    public void CopyFrom(CalendarEvent other)
    {
        Title = other.Title;
        Start = other.Start;
        End = other.End;
        IsAllDay = other.IsAllDay;
        Notes = other.Notes;
        ColorKey = other.ColorKey;
    }
}
