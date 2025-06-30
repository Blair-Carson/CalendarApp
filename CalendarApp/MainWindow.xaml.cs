using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SimpleCalendar
{
    public partial class MainWindow : Window
    {
        private DateTime _currentDate;
        private bool _isMonthView = true;
        private bool _isDarkTheme = false; // Track current theme state
        private double _miniCalendarDayFontSize = 10; // Default size

        public MainWindow()
        {
            InitializeComponent();
            _currentDate = DateTime.Today;
            ApplyTheme(false); // Apply light theme by default on startup
            UpdateDisplay();
        }

        private void ApplyTheme(bool isDark)
        {
            // Clear existing merged dictionaries (if any theme was loaded before)
            Application.Current.Resources.MergedDictionaries.Clear();

            // Load the new theme dictionary
            ResourceDictionary themeDictionary = new ResourceDictionary();
            if (isDark)
            {
                themeDictionary.Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative);
                _isDarkTheme = true;
            }
            else
            {
                themeDictionary.Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
                _isDarkTheme = false;
            }

            // Add the theme dictionary to the application's resources
            Application.Current.Resources.MergedDictionaries.Add(themeDictionary);

            // Reapply dynamic resources to elements that don't automatically update
            // (e.g., direct brush assignments instead of style references)
            // For example, if you had: myTextBlock.Foreground = Brushes.Black;
            // You'd need to re-set it like: myTextBlock.Foreground = (Brush)FindResource("PrimaryTextColor");
            // In your current code, most elements use styles which should update automatically with DynamicResource.
            // However, the "TodayHighlightColor" for DayNumberStyle and mini-calendar days is applied directly.
            // We need to re-evaluate it.
            UpdateDisplay(); // Re-render the calendar to pick up new colors
        }

        private void UpdateDisplay()
        {
            txtMonthYear.Text = _currentDate.ToString("MMMM yyyy");

            // Calculate current font sizes based on window size
            double baseSize = Math.Min(ActualWidth, ActualHeight) / 30;
            this.FontSize = Math.Max(12, Math.Min(baseSize, 24)); // Main font size

            _miniCalendarDayFontSize = Math.Max(6, Math.Min(baseSize * 0.7, 12)); // Mini calendar font size

            if (_isMonthView)
            {
                monthView.Visibility = Visibility.Visible;
                yearView.Visibility = Visibility.Collapsed;
                btnToggleView.Content = "Year View";
                DisplayMonth();
            }
            else
            {
                monthView.Visibility = Visibility.Collapsed;
                yearView.Visibility = Visibility.Visible;
                btnToggleView.Content = "Month View";
                DisplayYear();
            }
            btnToggleTheme.Content = _isDarkTheme ? "Light Mode" : "Dark Mode";
        }

        // Modify DisplayMonth
        private void DisplayMonth()
        {
            monthDaysGrid.Children.Clear();

            DateTime firstDay = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);
            int dayOfWeek = (int)firstDay.DayOfWeek;

            // Add empty cells for days before the first day of month
            for (int i = 0; i < dayOfWeek; i++)
            {
                monthDaysGrid.Children.Add(new TextBlock());
            }

            for (int day = 1; day <= daysInMonth; day++)
            {
                Border dayBorder = new Border
                {
                    Child = new TextBlock
                    {
                        Text = day.ToString(),
                        Style = (Style)FindResource("DayNumberStyle") // Still use style for other properties
                    },
                    Margin = new Thickness(2),
                    CornerRadius = new CornerRadius(10)
                };
                // Apply the FontSize directly to the TextBlock inside the Border
                // This takes precedence over a potentially fixed FontSize in DayNumberStyle
                ((TextBlock)dayBorder.Child).FontSize = this.FontSize; // Use main window font size for month view days

                if (day == DateTime.Today.Day && _currentDate.Month == DateTime.Today.Month && _currentDate.Year == DateTime.Today.Year)
                {
                    dayBorder.Background = (Brush)Application.Current.Resources["TodayHighlightColor"];
                }
                else
                {
                    dayBorder.Background = Brushes.Transparent;
                }
                monthDaysGrid.Children.Add(dayBorder);
            }

            while (monthDaysGrid.Children.Count < 42) // 6 weeks × 7 days
            {
                monthDaysGrid.Children.Add(new TextBlock());
            }
        }


        private void DisplayYear()
        {
            yearView.Children.Clear();

            for (int month = 1; month <= 12; month++)
            {
                Border monthBorder = new Border
                {
                    Margin = new Thickness(5),
                    Background = (Brush)Application.Current.Resources["MonthBlockBackgroundColor"],
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(5),
                    BorderBrush = (Brush)Application.Current.Resources["BorderColor"],
                    BorderThickness = new Thickness(1)
                };

                Grid monthGridContainer = new Grid();
                monthGridContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                monthGridContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                TextBlock monthHeader = new TextBlock
                {
                    Text = new DateTime(_currentDate.Year, month, 1).ToString("MMM"),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 5),
                    Foreground = (Brush)Application.Current.Resources["HeaderTextColor"],
                    FontSize = this.FontSize * 0.9
                };
                Grid.SetRow(monthHeader, 0);
                monthGridContainer.Children.Add(monthHeader);

                UniformGrid miniGrid = new UniformGrid
                {
                    Rows = 7,
                    Columns = 7,
                    Background = (Brush)Application.Current.Resources["MainBackgroundColor"]
                };
                Grid.SetRow(miniGrid, 1);

                string[] dayNames = { "S", "M", "T", "W", "T", "F", "S" };
                foreach (string day in dayNames)
                {
                    miniGrid.Children.Add(new TextBlock
                    {
                        Text = day,
                        Style = (Style)FindResource("MiniCalendarDayStyle"),
                        FontWeight = FontWeights.Bold,
                        FontSize = _miniCalendarDayFontSize
                    });
                }

                DateTime firstDay = new DateTime(_currentDate.Year, month, 1);
                int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, month);
                int dayOfWeek = (int)firstDay.DayOfWeek;

                for (int i = 0; i < dayOfWeek; i++)
                {
                    // For empty cells, just add a dummy TextBlock or a Border with a TextBlock inside
                    miniGrid.Children.Add(new Border
                    {
                        Child = new TextBlock
                        {
                            Style = (Style)FindResource("MiniCalendarDayStyle"),
                            FontSize = _miniCalendarDayFontSize
                        }
                    });
                }

                for (int day = 1; day <= daysInMonth; day++)
                {
                    TextBlock dayBlock = new TextBlock
                    {
                        Text = day.ToString(),
                        Style = (Style)FindResource("MiniCalendarDayStyle"),
                        FontSize = _miniCalendarDayFontSize
                    };

                    Border dayCellBorder = new Border // Create a Border for each day cell
                    {
                        Child = dayBlock, // Put the TextBlock inside the Border
                        Margin = new Thickness(1), // Small margin for spacing between cells
                        Padding = new Thickness(2), // Padding inside the border, around the text
                        CornerRadius = new CornerRadius(5) // Rounded corners for the highlight box
                    };

                    if (day == DateTime.Today.Day && month == DateTime.Today.Month && _currentDate.Year == DateTime.Today.Year)
                    {
                        dayCellBorder.Background = (Brush)Application.Current.Resources["TodayHighlightColor"]; // Apply highlight to the Border
                        dayBlock.FontWeight = FontWeights.Bold; // Keep text bold
                        dayBlock.Foreground = (Brush)Application.Current.Resources["DayNumberTextColor"]; // Ensure text color is theme-appropriate
                    }
                    else
                    {
                        dayCellBorder.Background = Brushes.Transparent; // Ensure non-highlighted days are transparent
                                                                        // Optionally reset foreground if you had special logic for today's foreground
                        dayBlock.Foreground = (Brush)Application.Current.Resources["MiniCalendarDayTextColor"]; // Ensure non-highlighted text color is theme-appropriate
                    }

                    miniGrid.Children.Add(dayCellBorder); // Add the Border to the miniGrid
                }

                while (miniGrid.Children.Count < 49)
                {
                    // For remaining empty cells, also use a Border
                    miniGrid.Children.Add(new Border
                    {
                        Child = new TextBlock
                        {
                            Style = (Style)FindResource("MiniCalendarDayStyle"),
                            FontSize = _miniCalendarDayFontSize
                        }
                    });
                }

                monthGridContainer.Children.Add(miniGrid);
                monthBorder.Child = monthGridContainer;
                yearView.Children.Add(monthBorder);
            }
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_isMonthView)
            {
                _currentDate = _currentDate.AddMonths(-1);
            }
            else
            {
                _currentDate = _currentDate.AddYears(-1);
            }
            UpdateDisplay();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_isMonthView)
            {
                _currentDate = _currentDate.AddMonths(1);
            }
            else
            {
                _currentDate = _currentDate.AddYears(1);
            }
            UpdateDisplay();
        }

        private void btnToggleView_Click(object sender, RoutedEventArgs e)
        {
            _isMonthView = !_isMonthView;
            UpdateDisplay();
        }

        // New event handler for theme toggle button
        private void btnToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme; // Toggle the theme state
            ApplyTheme(_isDarkTheme);    // Apply the new theme
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Adjust font size based on window dimensions
            double baseSize = Math.Min(ActualWidth, ActualHeight) / 30;
            this.FontSize = Math.Max(12, Math.Min(baseSize, 24));

            // No direct style modification here
            // The font size for mini-calendar days will be calculated and applied in DisplayYear/DisplayMonth

            // For year view, we need to redraw to adjust mini calendar sizes (if we aren't dynamically updating them)
            // For month view, we just need to update the display to refresh the layout if required by the new font size
            UpdateDisplay();
        }

        //private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    // Adjust font size based on window dimensions
        //    double baseSize = Math.Min(ActualWidth, ActualHeight) / 30;
        //    this.FontSize = Math.Max(6, Math.Min(baseSize, 24));

        //    // Adjust font size for mini calendar days as well
        //    if (!_isMonthView) // Only adjust for year view if needed
        //    {
        //        double miniCalendarFontSize = Math.Max(10, Math.Min(baseSize * 0.7, 12));
        //        // Update the style's setter for FontSize
        //        if (Application.Current.Resources.Contains("MiniCalendarDayStyle"))
        //        {
        //            Style miniCalendarDayStyle = (Style)Application.Current.Resources["MiniCalendarDayStyle"];
        //            // Find and update the existing FontSize setter or add a new one
        //            bool foundFontSizeSetter = false;
        //            foreach (Setter setter in miniCalendarDayStyle.Setters)
        //            {
        //                if (setter.Property == TextBlock.FontSizeProperty)
        //                {
        //                    setter.Value = miniCalendarFontSize;
        //                    foundFontSizeSetter = true;
        //                    break;
        //                }
        //            }
        //            if (!foundFontSizeSetter)
        //            {
        //                miniCalendarDayStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, miniCalendarFontSize));
        //            }
        //        }
        //    }

        //    // For year view, we need to redraw to adjust mini calendar sizes
        //    // Also needed for month view to ensure colors are reapplied correctly after resize and theme change
        //    UpdateDisplay();
        //}
    }
}