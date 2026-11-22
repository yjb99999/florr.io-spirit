using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using FlorrTeach.Services;

namespace FlorrTeach.Views;

public partial class CountdownWindow : Window
{
    private const int MaxMinutes = 99;

    private int _initial;
    private int _remaining;
    private bool _running;
    private readonly ThemePalette _theme;

    private readonly DispatcherTimer _timer = new();

    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint uType);

    public CountdownWindow(int initial = 300)
    {
        InitializeComponent();
        WindowExtras.MarkAsToolWindow(this);

        _initial = Math.Max(0, Math.Min(MaxMinutes * 60 + 59, initial));
        _remaining = _initial;
        _theme = ThemePalette.FromConfig(App.Config.Data.EffectiveTheme);
        ApplyTheme();

        InitPickers();

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => Tick();

        BtnToggle.Click += (_, _) => ToggleStartEnd();
        BtnPause.Click += (_, _) => TogglePause();
        BtnClose.Click += (_, _) => Close();

        MinutePicker.SelectionChanged += (_, _) => SyncRemainingFromPickers();
        SecondPicker.SelectionChanged += (_, _) => SyncRemainingFromPickers();

        TitleBar.MouseLeftButtonDown += (_, e) => DragMove();

        ShowPicker(true);
        UpdateDisplay();
    }

    private void ApplyTheme()
    {
        RootBorder.Background = _theme.CardBackground;
        TitleText.Foreground = _theme.PrimaryForeground;
        TimeText.Foreground = _theme.PrimaryForeground;
        StatusText.Foreground = _theme.SecondaryForeground;
        BtnToggle.Background = _theme.MutedBackground;
        BtnToggle.Foreground = _theme.MutedForeground;
        BtnPause.Background = _theme.MutedBackground;
        BtnPause.Foreground = _theme.MutedForeground;
        BtnClose.Foreground = _theme.SecondaryForeground;

        var maskColor = (_theme.CardBackground as SolidColorBrush)?.Color ?? Colors.Transparent;
        foreach (var picker in new[] { MinutePicker, SecondPicker })
        {
            picker.ForegroundBrush = _theme.SecondaryForeground;
            picker.HighlightBrush = _theme.PrimaryForeground;
            picker.SetMaskBackground(maskColor);
        }
    }

    private void InitPickers()
    {
        var minuteItems = new System.Collections.Generic.List<string>();
        for (var m = 0; m <= MaxMinutes; m++) minuteItems.Add($"{m:00}");

        var secondItems = new System.Collections.Generic.List<string>();
        for (var s = 0; s <= 59; s++) secondItems.Add($"{s:00}");

        var minutes = _remaining / 60;
        var seconds = _remaining % 60;
        MinutePicker.Init(minuteItems, minutes);
        SecondPicker.Init(secondItems, seconds);
    }

    private void SyncRemainingFromPickers()
    {
        if (_running) return;
        _remaining = MinutePicker.SelectedIndex * 60 + SecondPicker.SelectedIndex;
        UpdateDisplay();
    }

    private void ShowPicker(bool show)
    {
        PickerPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        TimeText.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateDisplay()
    {
        var s = Math.Max(0, _remaining);
        TimeText.Text = $"{s / 3600:00}:{(s % 3600) / 60:00}:{s % 60:00}";
    }

    private void Tick()
    {
        if (_remaining > 0)
        {
            _remaining--;
            UpdateDisplay();
            if (_remaining == 0)
            {
                _running = false;
                _timer.Stop();
                StatusText.Text = "⏰ 时间到";
                BtnPause.Content = "暂停";
                BtnToggle.Content = "开始";
                ShowPicker(true);
                MessageBeep(0xFFFFFFFF);
            }
        }
    }

    private void ToggleStartEnd()
    {
        if (_running) End();
        else Start();
    }

    private void Start()
    {
        if (_remaining <= 0) _remaining = MinutePicker.SelectedIndex * 60 + SecondPicker.SelectedIndex;
        _initial = _remaining;
        if (!_running)
        {
            _running = true;
            _timer.Start();
            StatusText.Text = "";
            BtnToggle.Content = "结束";
            ShowPicker(false);
        }
    }

    private void End()
    {
        _running = false;
        _timer.Stop();
        _remaining = _initial;
        UpdateDisplay();
        StatusText.Text = "";
        BtnPause.Content = "暂停";
        BtnToggle.Content = "开始";
        ShowPicker(true);
    }

    private void TogglePause()
    {
        if (_remaining <= 0) return;
        if (_running)
        {
            _running = false;
            _timer.Stop();
            BtnPause.Content = "继续";
            StatusText.Text = "已暂停";
        }
        else
        {
            _running = true;
            _timer.Start();
            BtnPause.Content = "暂停";
            StatusText.Text = "";
        }
    }
}
