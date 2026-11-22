using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FlorrTeach.Models;
using FlorrTeach.Services;

namespace FlorrTeach.Views;

public partial class DutyReminderWindow : Window
{
    private readonly List<DutyItem> _dutyList;
    private readonly string _dutyType;
    private readonly List<Button> _buttons = new();
    private readonly List<bool> _finished = new();
    private readonly ThemePalette _theme;

    public DutyReminderWindow(List<DutyItem> dutyList, string dutyType)
    {
        InitializeComponent();
        WindowExtras.MarkAsToolWindow(this);
        _dutyList = dutyList;
        _dutyType = dutyType;
        _theme = ThemePalette.FromConfig(App.Config.Data.EffectiveTheme);
        ApplyTheme();

        TitleText.Text = dutyType == "noon" ? "中午值日提醒" : "晚上值日提醒";

        // 仅标题栏可拖拽整窗
        TitleBar.MouseLeftButtonDown += (_, e) => DragMove();

        for (int i = 0; i < _dutyList.Count; i++)
        {
            var item = _dutyList[i];
            int index = i;
            var btn = new Button
            {
                Content = $"{item.Name} - {item.Role}",
                Height = 40,
                Margin = new Thickness(0, 0, 0, 8),
                Background = _theme.MutedBackground,
                Foreground = _theme.MutedForeground,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 14,
            };
            btn.Click += (_, _) => Toggle(index);
            ButtonPanel.Children.Add(btn);
            _buttons.Add(btn);
            _finished.Add(false);
        }

        BtnDone.Click += (_, _) => FinishDuty();
    }

    private void ApplyTheme()
    {
        RootBorder.Background = _theme.CardBackground;
        RootBorder.BorderBrush = _theme.CardBorder;
        TitleText.Foreground = _theme.PrimaryForeground;
        BtnDone.Background = _theme.Accent;
        BtnDone.Foreground = _theme.AccentForeground;
    }

    private void Toggle(int index)
    {
        _finished[index] = !_finished[index];
        var btn = _buttons[index];
        var baseText = ((string)btn.Content).Replace(" ✓", "");
        if (_finished[index])
        {
            btn.Content = baseText + " ✓";
            btn.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            btn.Foreground = Brushes.White;
        }
        else
        {
            btn.Content = baseText;
            btn.Background = _theme.MutedBackground;
            btn.Foreground = _theme.MutedForeground;
        }
    }

    private void FinishDuty()
    {
        var finished = new List<string>();
        var unfinished = new List<string>();

        for (int i = 0; i < _dutyList.Count; i++)
        {
            var item = _dutyList[i];
            var display = $"{item.Name}({item.Role})";
            if (_finished[i]) finished.Add(display);
            else unfinished.Add(display);
        }

        var now = DateTime.Now;
        var dateStr = now.ToString("yyyy年MM月dd日");
        var timeStr = _dutyType == "noon" ? "中午" : "晚上";
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var filePath = Path.Combine(desktop, $"值日记录_{dateStr}_{timeStr}.txt");

        var content = $"{dateStr} {timeStr}值日记录\n\n已完成：\n";
        foreach (var n in finished) content += $"  ✓ {n}\n";
        content += "\n未完成：\n";
        foreach (var n in unfinished) content += $"  ✗ {n}\n";

        try
        {
            File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "保存值日记录失败：" + ex.Message, "提示");
        }

        Close();
    }
}
