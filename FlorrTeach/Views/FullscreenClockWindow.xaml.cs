using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace FlorrTeach.Views;

public partial class FullscreenClockWindow : Window
{
    private readonly DispatcherTimer _timer = new();
    private readonly string _theme;

    private static readonly string[] Weekdays = { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };

    public FullscreenClockWindow(string theme = "black")
    {
        InitializeComponent();
        _theme = theme;
        ApplyTheme();

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => UpdateDisplay();
        _timer.Start();
        UpdateDisplay();

        KeyDown += (_, _) => Close();
        MouseDown += (_, _) => Close();
    }

    private void ApplyTheme()
    {
        if (_theme == "white")
        {
            Background = Brushes.White;
            TimeText.Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            DateText.Foreground = new SolidColorBrush(Color.FromRgb(90, 90, 90));
            TipText.Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140));
        }
        else
        {
            Background = Brushes.Black;
            TimeText.Foreground = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            DateText.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
            TipText.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
        }
    }

    private void UpdateDisplay()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString("HH:mm:ss");
        var week = Weekdays[(int)now.DayOfWeek];
        DateText.Text = $"{now:yyyy.MM.dd}   {week}";
    }
}
