using System.Windows;
using CalendarApp.Services;
using CalendarApp.ViewModels;

namespace CalendarApp.Views;

/// <inheritdoc/>
public sealed class DialogService : IDialogService
{
    /// <summary>Set once the main window exists so dialogs centre on it.</summary>
    public Window? Owner { get; set; }

    public bool ShowEventEditor(EventEditorViewModel editor)
    {
        var window = new EventEditorWindow(editor)
        {
            Owner = Owner,
        };

        return window.ShowDialog() == true;
    }

    public bool Confirm(string title, string message)
    {
        Window? owner = Owner ?? Application.Current?.MainWindow;

        MessageBoxResult result = owner is null
            ? MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
            : MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }
}
