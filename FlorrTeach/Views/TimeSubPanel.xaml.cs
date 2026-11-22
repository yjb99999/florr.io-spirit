using System;
using System.Windows;
using FlorrTeach.Services;

namespace FlorrTeach.Views;

public partial class TimeSubPanel : Window
{
    /// <summary>请求打开全屏时钟。</summary>
    public event Action? ClockRequested;

    /// <summary>请求打开倒计时窗口。</summary>
    public event Action? TimerRequested;

    public TimeSubPanel()
    {
        InitializeComponent();
        WindowExtras.MarkAsToolWindow(this);
        BtnClock.Click += (_, _) => ClockRequested?.Invoke();
        BtnTimer.Click += (_, _) => TimerRequested?.Invoke();
    }
}
