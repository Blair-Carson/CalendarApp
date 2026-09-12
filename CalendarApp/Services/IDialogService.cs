using CalendarApp.ViewModels;

namespace CalendarApp.Services;

/// <summary>Lets the view model raise windows without taking a reference to any of them.</summary>
public interface IDialogService
{
    /// <summary>Shows the event editor modally. Returns true when the user confirmed.</summary>
    bool ShowEventEditor(EventEditorViewModel editor);

    /// <summary>Shows a yes/no prompt. Returns true when the user agreed.</summary>
    bool Confirm(string title, string message);
}
