using System;
using System.ComponentModel;
using System.Windows;
using CalendarApp.ViewModels;

namespace CalendarApp.Views;

public partial class MainWindow : Window
{
    /// <summary>Rough vertical budget of one event chip, in device-independent pixels.</summary>
    private const double ChipHeight = 19;

    /// <summary>
    /// Everything in a cell that is not a chip: the 2px margin and 1.5px border on each
    /// side, the day-number badge row, and the "+n more" line.
    /// </summary>
    private const double CellChrome = 54;

    private const int GridRows = 6;

    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        TitleBarTheme.Attach(this);
    }

    /// <summary>
    /// Tells the view model how many chips a cell can hold at the current size. Only the
    /// bound counts change, so growing the window never rebuilds the grid.
    /// </summary>
    private void MonthGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.HeightChanged)
        {
            return;
        }

        double cellHeight = e.NewSize.Height / GridRows;
        _viewModel.MaxEventsPerCell = (int)Math.Floor((cellHeight - CellChrome) / ChipHeight);
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // RestoreBounds holds the pre-maximise size, which is what we want to reopen at.
        Rect bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;

        _viewModel.SaveWindowSize(bounds.Width, bounds.Height);
    }
}
