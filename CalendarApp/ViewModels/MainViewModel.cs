using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using CalendarApp.Common;
using CalendarApp.Models;
using CalendarApp.Services;

namespace CalendarApp.ViewModels;

/// <summary>
/// Drives the whole window. The month grid, the week-number column and the twelve year-view
/// thumbnails are all allocated once in the constructor and then re-pointed at new dates, so
/// paging a month or a year updates bound values instead of rebuilding controls.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    /// <summary>Six weeks of seven days covers every possible month layout.</summary>
    private const int MonthCellCount = 42;
    private const int WeekCount = 6;
    private const int DefaultEventsPerCell = 3;

    private readonly EventRepository _events;
    private readonly JsonFileStore<AppSettings> _settingsStore;
    private readonly AppSettings _settings;
    private readonly IDialogService _dialogs;

    private DateTime _anchor = DateTime.Today;
    private DateTime _selectedDate = DateTime.Today;
    private CalendarEvent? _selectedEvent;
    private bool _isMonthView = true;
    private int _maxEventsPerCell = DefaultEventsPerCell;

    public MainViewModel(
        EventRepository events,
        JsonFileStore<AppSettings> settingsStore,
        AppSettings settings,
        IDialogService dialogs)
    {
        _events = events;
        _settingsStore = settingsStore;
        _settings = settings;
        _dialogs = dialogs;

        Days = new ObservableCollection<DayCellViewModel>(
            Enumerable.Range(0, MonthCellCount).Select(_ => new DayCellViewModel()));
        WeekNumbers = new ObservableCollection<string>(Enumerable.Repeat(string.Empty, WeekCount));
        DayHeaders = new ObservableCollection<string>(Enumerable.Repeat(string.Empty, 7));
        ShortDayHeaders = new ObservableCollection<string>(Enumerable.Repeat(string.Empty, 7));
        Months = new ObservableCollection<MiniMonthViewModel>(
            Enumerable.Range(1, 12).Select(m => new MiniMonthViewModel(m)));
        SelectedDayEvents = new ObservableCollection<CalendarEvent>();

        PreviousCommand = new RelayCommand(GoToPrevious);
        NextCommand = new RelayCommand(GoToNext);
        TodayCommand = new RelayCommand(GoToToday);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        SelectDayCommand = new RelayCommand(p => SelectDate((p as DayCellViewModel)?.Date));
        OpenMonthCommand = new RelayCommand(p => OpenMonth(p as MiniMonthViewModel));
        NewEventCommand = new RelayCommand(p => CreateEvent(p as DayCellViewModel));
        EditEventCommand = new RelayCommand(
            p => EditEvent(ResolveEvent(p)),
            p => ResolveEvent(p) is not null);
        DeleteEventCommand = new RelayCommand(
            p => DeleteEvent(ResolveEvent(p)),
            p => ResolveEvent(p) is not null);

        _events.Changed += (_, _) => Refresh();

        RebuildDayHeaders();

        Refresh();
        SelectDate(DateTime.Today);
    }

    public ObservableCollection<DayCellViewModel> Days { get; }

    public ObservableCollection<string> WeekNumbers { get; }

    public ObservableCollection<string> DayHeaders { get; }

    /// <summary>Single-letter headings for the cramped year-view thumbnails.</summary>
    public ObservableCollection<string> ShortDayHeaders { get; }

    public ObservableCollection<MiniMonthViewModel> Months { get; }

    public ObservableCollection<CalendarEvent> SelectedDayEvents { get; }

    public ICommand PreviousCommand { get; }

    public ICommand NextCommand { get; }

    public ICommand TodayCommand { get; }

    public ICommand ToggleThemeCommand { get; }

    public ICommand SelectDayCommand { get; }

    public ICommand OpenMonthCommand { get; }

    public ICommand NewEventCommand { get; }

    public ICommand EditEventCommand { get; }

    public ICommand DeleteEventCommand { get; }

    public IReadOnlyList<DayOfWeek> FirstDayOptions { get; } = new[]
    {
        DayOfWeek.Sunday,
        DayOfWeek.Monday,
        DayOfWeek.Saturday,
    };

    /// <summary>Large heading: the month and year in month view, just the year in year view.</summary>
    public string HeaderTitle => _isMonthView
        ? _anchor.ToString("MMMM yyyy", CultureInfo.CurrentCulture)
        : _anchor.ToString("yyyy", CultureInfo.CurrentCulture);

    public string TodayLabel => DateTime.Today.ToString("dddd, d MMMM yyyy", CultureInfo.CurrentCulture);

    public bool IsMonthView
    {
        get => _isMonthView;
        set
        {
            if (SetProperty(ref _isMonthView, value))
            {
                OnPropertyChanged(nameof(IsYearView));
                OnPropertyChanged(nameof(HeaderTitle));
                OnPropertyChanged(nameof(PreviousTooltip));
                OnPropertyChanged(nameof(NextTooltip));
                Refresh();
            }
        }
    }

    public bool IsYearView
    {
        get => !_isMonthView;
        set => IsMonthView = !value;
    }

    public string PreviousTooltip => _isMonthView ? "Previous month (Left arrow)" : "Previous year (Left arrow)";

    public string NextTooltip => _isMonthView ? "Next month (Right arrow)" : "Next year (Right arrow)";

    public bool IsDarkTheme => _settings.Theme == AppTheme.Dark;

    /// <summary>Segoe icon shown on the theme button: a sun while dark, a moon while light.</summary>
    public string ThemeIcon => IsDarkTheme ? "" : "";

    public string ThemeTooltip => IsDarkTheme ? "Switch to light theme" : "Switch to dark theme";

    public DayOfWeek FirstDayOfWeek
    {
        get => _settings.FirstDayOfWeek;
        set
        {
            if (_settings.FirstDayOfWeek == value)
            {
                return;
            }

            _settings.FirstDayOfWeek = value;
            OnPropertyChanged();
            RebuildDayHeaders();
            Refresh();
            SaveSettings();
        }
    }

    public bool ShowWeekNumbers
    {
        get => _settings.ShowWeekNumbers;
        set
        {
            if (_settings.ShowWeekNumbers == value)
            {
                return;
            }

            _settings.ShowWeekNumbers = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    /// <summary>The day the agenda panel is showing. Cells derive their highlight from this.</summary>
    public DateTime SelectedDate
    {
        get => _selectedDate;
        private set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                OnPropertyChanged(nameof(SelectedDayTitle));
                OnPropertyChanged(nameof(SelectedDayRelative));
            }
        }
    }

    public CalendarEvent? SelectedEvent
    {
        get => _selectedEvent;
        set => SetProperty(ref _selectedEvent, value);
    }

    public string SelectedDayTitle => _selectedDate.ToString("dddd, d MMMM", CultureInfo.CurrentCulture);

    /// <summary>A friendly qualifier under the agenda heading, e.g. "Today" or "In 3 days".</summary>
    public string SelectedDayRelative
    {
        get
        {
            int offset = (_selectedDate - DateTime.Today).Days;
            return offset switch
            {
                0 => "Today",
                1 => "Tomorrow",
                -1 => "Yesterday",
                > 1 => $"In {offset} days",
                _ => $"{-offset} days ago",
            };
        }
    }

    public bool HasSelectedDayEvents => SelectedDayEvents.Count > 0;

    /// <summary>
    /// How many event chips a single day cell has room for. The view feeds this in from its
    /// own height so a taller window shows more per day without any control being recreated.
    /// </summary>
    public int MaxEventsPerCell
    {
        get => _maxEventsPerCell;
        set
        {
            int clamped = Math.Clamp(value, 1, 6);
            if (SetProperty(ref _maxEventsPerCell, clamped) && _isMonthView)
            {
                RefreshMonthCells();
            }
        }
    }

    /// <summary>Persists window size on shutdown.</summary>
    public void SaveWindowSize(double width, double height)
    {
        _settings.WindowWidth = width;
        _settings.WindowHeight = height;
        SaveSettings();
    }

    private void GoToPrevious() => MoveTo(_isMonthView ? _anchor.AddMonths(-1) : _anchor.AddYears(-1));

    private void GoToNext() => MoveTo(_isMonthView ? _anchor.AddMonths(1) : _anchor.AddYears(1));

    private void GoToToday() => MoveTo(DateTime.Today);

    /// <summary>
    /// Moves the calendar to the period containing <paramref name="date"/> and parks the
    /// selection on a sensible day there, so the highlight is never off screen.
    /// </summary>
    private void MoveTo(DateTime date)
    {
        _anchor = date;
        OnPropertyChanged(nameof(HeaderTitle));
        Refresh();
        SelectDate(DefaultDayOf(_anchor));
    }

    /// <summary>Today when the shown month contains it, otherwise the first of that month.</summary>
    private static DateTime DefaultDayOf(DateTime anchor) =>
        anchor.Year == DateTime.Today.Year && anchor.Month == DateTime.Today.Month
            ? DateTime.Today
            : new DateTime(anchor.Year, anchor.Month, 1);

    private void ToggleTheme()
    {
        _settings.Theme = ThemeManager.Toggle();
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(ThemeIcon));
        OnPropertyChanged(nameof(ThemeTooltip));
        SaveSettings();
    }

    private void SelectDate(DateTime? date)
    {
        if (date is null)
        {
            return;
        }

        SelectedDate = date.Value.Date;
        ApplySelectionHighlight();
        RefreshAgenda();
    }

    private void ApplySelectionHighlight()
    {
        foreach (DayCellViewModel day in Days)
        {
            day.IsSelected = day.Date == _selectedDate;
        }
    }

    private void OpenMonth(MiniMonthViewModel? month)
    {
        if (month is null)
        {
            return;
        }

        _anchor = new DateTime(_anchor.Year, month.Month, 1);
        OnPropertyChanged(nameof(HeaderTitle));

        // Setting IsMonthView refreshes the grid; the selection then lands inside it.
        IsMonthView = true;
        SelectDate(DefaultDayOf(_anchor));
    }

    private void CreateEvent(DayCellViewModel? day)
    {
        DateTime date = day?.Date ?? _selectedDate;
        DateTime start = date.Date.AddHours(9);

        var draft = new CalendarEvent
        {
            Title = string.Empty,
            Start = start,
            End = start.AddHours(1),
        };

        if (_dialogs.ShowEventEditor(new EventEditorViewModel(draft, isNew: true)))
        {
            _events.Add(draft);
            SelectedEvent = draft;
        }
    }

    private void EditEvent(CalendarEvent? target)
    {
        if (target is null)
        {
            return;
        }

        // Edit a detached copy so cancelling cannot leave half-applied changes behind.
        CalendarEvent draft = target.Clone();

        if (_dialogs.ShowEventEditor(new EventEditorViewModel(draft, isNew: false)))
        {
            target.CopyFrom(draft);
            _events.Update(target);
            SelectedEvent = target;
        }
    }

    private void DeleteEvent(CalendarEvent? target)
    {
        if (target is null)
        {
            return;
        }

        if (_dialogs.Confirm("Delete event", $"Delete {target.Title}?"))
        {
            _events.Remove(target);
        }
    }

    /// <summary>Commands can be invoked with an explicit event, or fall back to the agenda selection.</summary>
    private CalendarEvent? ResolveEvent(object? parameter) => parameter as CalendarEvent ?? _selectedEvent;

    private void Refresh()
    {
        if (_isMonthView)
        {
            RefreshMonthCells();
        }
        else
        {
            RefreshYearCells();
        }

        RefreshAgenda();
    }

    private void RefreshMonthCells()
    {
        DateTime firstOfMonth = new(_anchor.Year, _anchor.Month, 1);
        DateTime cursor = StartOfGrid(firstOfMonth);

        for (int i = 0; i < MonthCellCount; i++)
        {
            DateTime date = cursor.AddDays(i);
            Days[i].Update(
                date,
                date.Month == firstOfMonth.Month && date.Year == firstOfMonth.Year,
                _events.EventsOn(date),
                _maxEventsPerCell);
        }

        for (int week = 0; week < WeekCount; week++)
        {
            // Take the midweek day so the row's ISO week number is right whichever
            // weekday the user starts their week on.
            WeekNumbers[week] = ISOWeek.GetWeekOfYear(cursor.AddDays((week * 7) + 3))
                .ToString(CultureInfo.CurrentCulture);
        }

        ApplySelectionHighlight();
    }

    private void RefreshYearCells()
    {
        foreach (MiniMonthViewModel month in Months)
        {
            DateTime firstOfMonth = new(_anchor.Year, month.Month, 1);
            month.Name = firstOfMonth.ToString("MMMM", CultureInfo.CurrentCulture);
            month.IsCurrentMonth = firstOfMonth.Year == DateTime.Today.Year && firstOfMonth.Month == DateTime.Today.Month;

            DateTime cursor = StartOfGrid(firstOfMonth);
            for (int i = 0; i < MiniMonthViewModel.CellCount; i++)
            {
                DateTime date = cursor.AddDays(i);
                if (date.Month == firstOfMonth.Month && date.Year == firstOfMonth.Year)
                {
                    month.Days[i].SetDay(date, _events.EventsOn(date).Count > 0);
                }
                else
                {
                    month.Days[i].Clear();
                }
            }
        }
    }

    private void RefreshAgenda()
    {
        SelectedDayEvents.Clear();

        foreach (CalendarEvent item in _events.EventsOn(_selectedDate))
        {
            SelectedDayEvents.Add(item);
        }

        if (_selectedEvent is not null && !SelectedDayEvents.Contains(_selectedEvent))
        {
            SelectedEvent = null;
        }

        OnPropertyChanged(nameof(HasSelectedDayEvents));
        OnPropertyChanged(nameof(SelectedDayTitle));
        OnPropertyChanged(nameof(SelectedDayRelative));
    }

    private void RebuildDayHeaders()
    {
        DateTimeFormatInfo format = CultureInfo.CurrentCulture.DateTimeFormat;

        for (int i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)(((int)_settings.FirstDayOfWeek + i) % 7);
            DayHeaders[i] = format.AbbreviatedDayNames[(int)day].ToUpper(CultureInfo.CurrentCulture);
            ShortDayHeaders[i] = format.ShortestDayNames[(int)day].ToUpper(CultureInfo.CurrentCulture);
        }
    }

    /// <summary>The Sunday/Monday/Saturday on or before the first of the month, per the user's preference.</summary>
    private DateTime StartOfGrid(DateTime firstOfMonth)
    {
        int offset = ((int)firstOfMonth.DayOfWeek - (int)_settings.FirstDayOfWeek + 7) % 7;
        return firstOfMonth.AddDays(-offset);
    }

    private void SaveSettings() => _settingsStore.Save(_settings);
}
