using System.Windows;
using CalendarApp.ViewModels;

namespace CalendarApp.Views;

public partial class EventEditorWindow : Window
{
    private readonly EventEditorViewModel _viewModel;

    public EventEditorWindow(EventEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        TitleBarTheme.Attach(this);

        Loaded += (_, _) =>
        {
            TitleBox.Focus();
            TitleBox.SelectAll();
        };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // The view model reports what is wrong; the dialog stays open until it is happy.
        if (_viewModel.TryCommit())
        {
            DialogResult = true;
        }
    }
}
