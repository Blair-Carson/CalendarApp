using System.Collections.Generic;
using System.Windows;
using CalendarApp.Models;
using CalendarApp.Services;
using CalendarApp.ViewModels;
using CalendarApp.Views;

namespace CalendarApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsStore = new JsonFileStore<AppSettings>("settings.json");
        AppSettings settings = settingsStore.Load() ?? new AppSettings();

        ThemeManager.Apply(settings.Theme);

        var eventStore = new JsonFileStore<List<CalendarEvent>>("events.json");
        var repository = new EventRepository(eventStore);

        var dialogs = new DialogService();
        var viewModel = new MainViewModel(repository, settingsStore, settings, dialogs);

        var window = new MainWindow(viewModel)
        {
            Width = settings.WindowWidth,
            Height = settings.WindowHeight,
        };

        dialogs.Owner = window;
        MainWindow = window;
        window.Show();
    }
}
