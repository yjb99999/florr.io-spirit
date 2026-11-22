using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FlorrTeach.Services;

namespace FlorrTeach.Views;

public partial class RollCallDialog : Window
{
    private readonly List<string> _students;
    private readonly string _className;
    private bool _rolling;
    private readonly Random _rng = new();
    private readonly ThemePalette _theme;

    private readonly DispatcherTimer _rollTimer = new();
    private readonly DispatcherTimer _autoStopTimer = new();

    public RollCallDialog(List<string> students, string className = "")
    {
        InitializeComponent();
        WindowExtras.MarkAsToolWindow(this);
        _students = students ?? new List<string>();
        _className = className;
        _theme = ThemePalette.FromConfig(App.Config.Data.EffectiveTheme);
        ApplyTheme();

        TitleText.Text = string.IsNullOrEmpty(className) ? "随机点名" : $"随机点名 · {className}";

        // 仅标题栏可拖拽整窗
        TitleBar.MouseLeftButtonDown += (_, e) => DragMove();

        _rollTimer.Interval = TimeSpan.FromMilliseconds(60);
        _rollTimer.Tick += (_, _) => RollOnce();

        _autoStopTimer.Interval = TimeSpan.FromMilliseconds(2000);
        _autoStopTimer.Tick += (_, _) => AutoStop();

        BtnPick.Click += (_, _) => TogglePick();
        BtnClose.Click += (_, _) => Close();
    }

    private void ApplyTheme()
    {
        RootBorder.Background = _theme.CardBackground;
        RootBorder.BorderBrush = _theme.CardBorder;
        TitleText.Foreground = _theme.PrimaryForeground;
        NameBox.Background = _theme.MutedBackground;
        NameText.Foreground = _theme.PrimaryForeground;
        BtnPick.Background = _theme.Accent;
        BtnPick.Foreground = _theme.AccentForeground;
        BtnClose.Background = _theme.MutedBackground;
        BtnClose.Foreground = _theme.MutedForeground;
    }

    private void TogglePick()
    {
        if (_students.Count == 0)
        {
            MessageBox.Show(this, "尚未导入学生名单，请先在「设置」中导入。", "提示");
            return;
        }

        if (_rolling)
        {
            StopRolling();
            NameBox.Background = _theme.MutedBackground;
            NameText.Foreground = _theme.Accent;
        }
        else
        {
            _rolling = true;
            BtnPick.Content = "停 止";
            NameBox.Background = _theme.MutedBackground;
            NameText.Foreground = _theme.PrimaryForeground;
            _rollTimer.Start();
            _autoStopTimer.Start();
        }
    }

    private void StopRolling()
    {
        _rolling = false;
        _rollTimer.Stop();
        _autoStopTimer.Stop();
        BtnPick.Content = "开始抽取";
    }

    private void AutoStop()
    {
        if (_rolling) TogglePick();
    }

    private void RollOnce()
    {
        NameText.Text = _students[_rng.Next(_students.Count)];
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _rollTimer.Stop();
        _autoStopTimer.Stop();
        base.OnClosing(e);
    }
}
