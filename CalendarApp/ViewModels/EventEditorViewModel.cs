using System;
using System.Collections.Generic;
using System.Globalization;
using CalendarApp.Common;
using CalendarApp.Models;

namespace CalendarApp.ViewModels;

/// <summary>Backs the add/edit event dialog. Edits a copy so Cancel leaves the original untouched.</summary>
public sealed class EventEditorViewModel : ObservableObject
{
    private const int TimeStepMinutes = 30;

    private string _title;
    private DateTime _startDate;
    private DateTime _endDate;
    private string _startTime;
    private string _endTime;
    private bool _isAllDay;
    private string _notes;
    private string _colorKey;
    private string _errorMessage = string.Empty;

    public EventEditorViewModel(CalendarEvent source, bool isNew)
    {
        Source = source;
        IsNew = isNew;

        _title = source.Title;
        _startDate = source.Start.Date;
        _endDate = source.End.Date;
        _startTime = FormatTime(source.Start);
        _endTime = FormatTime(source.End);
        _isAllDay = source.IsAllDay;
        _notes = source.Notes;
        _colorKey = source.ColorKey;

        TimeOptions = BuildTimeOptions();
    }

    /// <summary>The event this dialog writes back to when the user confirms.</summary>
    public CalendarEvent Source { get; }

    public bool IsNew { get; }

    public string WindowTitle => IsNew ? "New event" : "Edit event";

    public IReadOnlyList<string> TimeOptions { get; }

    public IReadOnlyList<string> ColorKeys => EventPalette.Keys;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public DateTime StartDate
    {
        get => _startDate;
        set
        {
            if (SetProperty(ref _startDate, value) && _endDate < value)
            {
                EndDate = value;
            }
        }
    }

    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public string StartTime
    {
        get => _startTime;
        set => SetProperty(ref _startTime, value);
    }

    public string EndTime
    {
        get => _endTime;
        set => SetProperty(ref _endTime, value);
    }

    public bool IsAllDay
    {
        get => _isAllDay;
        set
        {
            if (SetProperty(ref _isAllDay, value))
            {
                OnPropertyChanged(nameof(ShowTimes));
            }
        }
    }

    public bool ShowTimes => !_isAllDay;

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string ColorKey
    {
        get => _colorKey;
        set => SetProperty(ref _colorKey, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => _errorMessage.Length > 0;

    /// <summary>
    /// Validates the form and, when it is sound, writes the values onto <see cref="Source"/>.
    /// Returns false and populates <see cref="ErrorMessage"/> otherwise.
    /// </summary>
    public bool TryCommit()
    {
        string title = (_title ?? string.Empty).Trim();
        if (title.Length == 0)
        {
            ErrorMessage = "Give the event a title.";
            return false;
        }

        DateTime start;
        DateTime end;

        if (_isAllDay)
        {
            start = _startDate.Date;
            end = _endDate.Date;
        }
        else
        {
            if (!TryParseTime(_startTime, out TimeSpan startOfDay))
            {
                ErrorMessage = "The start time is not a time we recognise.";
                return false;
            }

            if (!TryParseTime(_endTime, out TimeSpan endOfDay))
            {
                ErrorMessage = "The end time is not a time we recognise.";
                return false;
            }

            start = _startDate.Date + startOfDay;
            end = _endDate.Date + endOfDay;
        }

        if (end < start)
        {
            ErrorMessage = "The event cannot end before it starts.";
            return false;
        }

        ErrorMessage = string.Empty;

        Source.Title = title;
        Source.Start = start;
        Source.End = end;
        Source.IsAllDay = _isAllDay;
        Source.Notes = (_notes ?? string.Empty).Trim();
        Source.ColorKey = _colorKey;
        return true;
    }

    private static string FormatTime(DateTime value) => value.ToString("t", CultureInfo.CurrentCulture);

    private static bool TryParseTime(string? text, out TimeSpan result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();

        // Accept whatever the culture's short-time format produces, plus the terser
        // forms people type by hand, such as "9" or "9:30".
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime parsed) ||
            DateTime.TryParseExact(
                text,
                new[] { "H", "HH", "H:mm", "HH:mm", "h tt", "htt", "h:mm tt", "hh:mm tt" },
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out parsed))
        {
            result = parsed.TimeOfDay;
            return true;
        }

        return false;
    }

    private static IReadOnlyList<string> BuildTimeOptions()
    {
        var options = new List<string>(24 * 60 / TimeStepMinutes);
        DateTime cursor = DateTime.Today;
        DateTime endOfDay = cursor.AddDays(1);

        while (cursor < endOfDay)
        {
            options.Add(FormatTime(cursor));
            cursor = cursor.AddMinutes(TimeStepMinutes);
        }

        return options;
    }
}
