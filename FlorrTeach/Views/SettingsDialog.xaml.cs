using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using FlorrTeach.Services;

namespace FlorrTeach.Views;

public partial class SettingsDialog : Window
{
    private const string DevKey1 = "yjb999";
    private const string DevKey2 = "121225";

    private readonly FloatingBallWindow _ball;

    public SettingsDialog(FloatingBallWindow ball)
    {
        InitializeComponent();
        // 设置窗口保留在任务栏 / Alt+Tab 中，被遮挡后可点回（其余附属窗口为工具窗口保持隐藏）
        ShowInTaskbar = true;
        _ball = ball;
        LoadFromConfig();

        BtnOpenName.Click += (_, _) => OpenNameFile();
        BtnOpenDuty.Click += (_, _) => OpenDutyFile();
        BtnRefresh.Click += (_, _) => RefreshApp();
        BtnOk.Click += (_, _) => SaveAndClose();
        BtnDevActivate.Click += (_, _) => ActivateDev();
        BtnPickIcon.Click += (_, _) => PickIcon();

        RadioFreqCustom.Checked += (_, _) => CustomRow.IsEnabled = true;
        RadioFreqCustom.Unchecked += (_, _) => CustomRow.IsEnabled = false;

        BallSizeSlider.ValueChanged += (_, _) =>
            BallSizeLabel.Text = $"{(int)Math.Round(BallSizeSlider.Value)} px";
    }

    private void LoadFromConfig()
    {
        var cfg = App.Config.Data;

        RadioWhite.IsChecked = cfg.TimeTheme == "white";
        RadioBlack.IsChecked = cfg.TimeTheme != "white";

        ClassLabel.Text = string.IsNullOrEmpty(cfg.ClassName)
            ? "班级：未设置" : $"班级：{cfg.ClassName}（{cfg.Students.Count}人）";

        RadioSizeSmall.IsChecked = cfg.BallSize == "small";
        RadioSizeMedium.IsChecked = cfg.BallSize == "medium";
        RadioSizeLarge.IsChecked = cfg.BallSize == "large";

        ChkDuty.IsChecked = cfg.DutyEnabled;
        NoonTime.Text = cfg.NoonDutyTime;
        EveningTime.Text = cfg.EveningDutyTime;
        DutyStatus.Text = cfg.DutyData.Count > 0 ? $"已设置 {cfg.DutyData.Count} 天值日表" : "未设置值日表";

        KillEdit.Text = string.Join("\n", cfg.KillProcesses);

        RadioFreq5.IsChecked = cfg.KillFreq == "5s";
        RadioFreq30.IsChecked = cfg.KillFreq == "30s";
        RadioFreqBoot.IsChecked = cfg.KillFreq == "boot";
        RadioFreqCustom.IsChecked = cfg.KillFreq == "custom";
        RadioFreq10.IsChecked = cfg.KillFreq == "10s" ||
            (!RadioFreq5.IsChecked.Value && !RadioFreq30.IsChecked.Value &&
             !RadioFreqBoot.IsChecked.Value && !RadioFreqCustom.IsChecked.Value);
        KillIntervalEdit.Text = cfg.KillIntervalSeconds >= 1 ? cfg.KillIntervalSeconds.ToString() : "10";
        CustomRow.IsEnabled = RadioFreqCustom.IsChecked == true;

        RadioThemeFollow.IsChecked = string.IsNullOrEmpty(cfg.PanelTheme);
        RadioThemeBlack.IsChecked = cfg.PanelTheme == "black";
        RadioThemeWhite.IsChecked = cfg.PanelTheme == "white";
        var px = cfg.BallSizePx >= 40 && cfg.BallSizePx <= 100 ? cfg.BallSizePx : 64;
        BallSizeSlider.Value = px;
        BallSizeLabel.Text = $"{px} px";

        if (cfg.DevMode)
        {
            DevLockedPanel.Visibility = Visibility.Collapsed;
            DevUnlockedPanel.Visibility = Visibility.Visible;
            RefreshIconLabel();
        }
        else
        {
            DevLockedPanel.Visibility = Visibility.Visible;
            DevUnlockedPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void ActivateDev()
    {
        var pwd1 = DevPwd1.Password ?? "";
        var pwd2 = DevPwd2.Password ?? "";

        if (pwd1 != DevKey1)
        {
            MessageBox.Show(this, "第一密码错误。", "提示");
            return;
        }
        if (pwd2 != DevKey2)
        {
            MessageBox.Show(this, "第二密码错误。", "提示");
            return;
        }

        App.Config.Data.DevMode = true;
        App.Config.Save();
        DevLockedPanel.Visibility = Visibility.Collapsed;
        DevUnlockedPanel.Visibility = Visibility.Visible;
        RefreshIconLabel();
    }

    private void PickIcon()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "图标 (*.png)|*.png|所有文件 (*.*)|*.*",
            Title = "选择悬浮球图标"
        };
        if (dlg.ShowDialog() != true) return;

        App.Config.Data.BallIcon = dlg.FileName;
        App.Config.Save();
        RefreshIconLabel();
        _ball.ApplySettings();
    }

    private void RefreshIconLabel()
    {
        IconPathLabel.Text = string.IsNullOrEmpty(App.Config.Data.BallIcon)
            ? "未自定义，使用默认图标" : App.Config.Data.BallIcon;
    }

    private void OpenNameFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "名单文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            Title = "选择学生名单"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var (className, students) = FileParsers.ParseStudentsFile(dlg.FileName);
            App.Config.Data.ClassName = className;
            App.Config.Data.Students = students;
            App.Config.Save();
            ClassLabel.Text = string.IsNullOrEmpty(className)
                ? "班级：未设置" : $"班级：{className}（{students.Count}人）";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "读取名单失败：" + ex.Message, "提示");
        }
    }

    private void OpenDutyFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "值日表 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            Title = "选择值日表"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            App.Config.Data.DutyData = FileParsers.ParseDutyFile(dlg.FileName);
            App.Config.Save();
            DutyStatus.Text = $"已设置 {App.Config.Data.DutyData.Count} 天值日表";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "读取值日表失败：" + ex.Message, "提示");
        }
    }

    private void RefreshApp()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe)
                {
                    UseShellExecute = true
                });
                Application.Current.Shutdown();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "刷新失败：" + ex.Message, "提示");
        }
    }

    private void SaveAndClose()
    {
        var cfg = App.Config.Data;

        cfg.TimeTheme = RadioWhite.IsChecked == true ? "white" : "black";

        cfg.BallSize = RadioSizeSmall.IsChecked == true ? "small"
            : RadioSizeLarge.IsChecked == true ? "large" : "medium";

        cfg.DutyEnabled = ChkDuty.IsChecked == true;
        cfg.NoonDutyTime = (NoonTime.Text ?? "").Trim();
        cfg.EveningDutyTime = (EveningTime.Text ?? "").Trim();

        var kill = new List<string>();
        foreach (var line in (KillEdit.Text ?? "").Split('\n'))
        {
            var t = line.Trim();
            if (!string.IsNullOrEmpty(t)) kill.Add(t);
        }
        cfg.KillProcesses = kill;

        if (RadioFreqCustom.IsChecked == true)
        {
            cfg.KillFreq = "custom";
            int.TryParse((KillIntervalEdit.Text ?? "").Trim(), out int sec);
            cfg.KillIntervalSeconds = sec >= 1 ? Math.Min(sec, 3600) : 10;
        }
        else
        {
            cfg.KillFreq = RadioFreq5.IsChecked == true ? "5s"
                : RadioFreq30.IsChecked == true ? "30s"
                : RadioFreqBoot.IsChecked == true ? "boot" : "10s";
        }

        cfg.PanelTheme = RadioThemeBlack.IsChecked == true ? "black"
            : RadioThemeWhite.IsChecked == true ? "white" : "";
        cfg.BallSizePx = cfg.DevMode ? (int)Math.Round(BallSizeSlider.Value) : 0;

        App.Config.Save();
        _ball.ApplySettings();
        Close();
    }
}
