# CalendarApp

A small Windows calendar for .NET 8 / WPF. Month and year views, events with notes and
colours, light and dark themes. No third-party dependencies.

## Running it

```
dotnet run --project CalendarApp
```

Or open `CalendarApp.sln` in Visual Studio and press F5.

## What it does

- **Month view** — six weeks at a time, including the greyed-out days either side of the
  month. Each day shows as many event chips as the cell has room for, then `+n more`.
- **Year view** — twelve month thumbnails with a dot on any day that has something on it.
  Click a month to open it.
- **Agenda panel** — the selected day's events in order, all-day first. Hovering a row
  reveals edit and delete buttons.
- **Events** — title, start and end (they may span days), all-day, notes, and one of seven
  colours. Double-clicking a day starts a new event on it.
- **Themes** — light and dark, including the window title bar. The choice is remembered.
- **Options** — first day of the week and ISO week numbers, both remembered.

### Keyboard

| Key | Action |
| --- | --- |
| `←` / `→`, `PgUp` / `PgDn` | Previous / next month (or year) |
| `T` or `Home` | Jump to today |
| `Ctrl`+`N` | New event on the selected day |
| `F2` | Edit the selected event |
| `Del` | Delete the selected event |
| `Ctrl`+`D` | Toggle the theme |

## Where data lives

`%AppData%\CalendarApp\` — `events.json` and `settings.json`. Writes go through a
temporary file and a replace, so an interrupted save cannot truncate good data; a file
that cannot be parsed is set aside as `.corrupt` rather than overwritten.

## Layout

```
CalendarApp/
  Common/       ObservableObject, RelayCommand
  Models/       CalendarEvent, EventPalette, AppSettings
  Services/     JSON storage, event repository, theme manager, dialog service
  ViewModels/   MainViewModel, DayCellViewModel, MiniMonthViewModel, EventEditorViewModel
  Views/        MainWindow, EventEditorWindow, DialogService, TitleBarTheme
  Converters/   Value converters used by the templates
  Themes/       Palette.Light.xaml, Palette.Dark.xaml, Styles.xaml
```

Two conventions are worth knowing before editing the UI:

- **Palettes hold colours, `Styles.xaml` holds everything else.** Styles reach colours
  through `DynamicResource`, so switching theme swaps one dictionary (slot 0 of
  `App.xaml`'s merged dictionaries) and every brush follows. Nothing is rebuilt.
- **Cells are recycled, not recreated.** The 42 month cells and the 12 x 42 year-view
  cells are allocated once in `MainViewModel` and re-pointed at new dates. Paging a month
  updates bound values rather than regenerating controls.
