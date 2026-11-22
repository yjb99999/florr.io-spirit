using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FlorrTeach.Models;
using FlorrTeach.Services;

namespace FlorrTeach.Views;

public partial class FloatingBallWindow : Window
{
    private readonly AppConfig _cfg;
    private ModulePanelWindow? _panel;
    private TimeSubPanel? _timeSub;
    private VolumeSubPanel? _volSub;
    private GlobalMouseHook? _mouseHook;

    // 保持引用防止被 GC 回收（对齐 Python self._clock）
    private FullscreenClockWindow? _clockWin;
    private CountdownWindow? _countWin;

    private Point? _pressPos;
    private Point? _dragOffset;
    private bool _moved;

    private readonly DispatcherTimer _killTimer = new();
    private readonly DispatcherTimer _dutyTimer = new();

    // 值日提醒状态（对齐 Python：启动时已过的时间标记为已提醒，不补弹）
    private DateTime? _lastNoonReminded;
    private DateTime? _lastEveningReminded;
    private bool _dutyWindowShowing;

    // 悬浮球尺寸（像素），与设置页文案 48 / 64 / 80 一致
    private static readonly System.Collections.Generic.Dictionary<string, int> SizeMap = new()
    {
        ["small"] = 48, ["medium"] = 64, ["large"] = 80,
    };

    public FloatingBallWindow()
    {
        InitializeComponent();
        _cfg = App.Config.Data;
        WindowExtras.MarkAsToolWindow(this);

        LoadBallImage();
        ApplyBallSize();
        PlaceInitial();

        _panel = new ModulePanelWindow(this);
        WirePanel();

        _timeSub = new TimeSubPanel();
        _volSub = new VolumeSubPanel();
        _timeSub.ClockRequested += OpenClock;
        _timeSub.TimerRequested += OpenCountdown;

        // 全局钩子：点击本应用之外的窗口时收起面板（替代不可靠的 Deactivated 失活收回）
        _mouseHook = new GlobalMouseHook();
        _mouseHook.MouseClickedOutside += () => HideAll();
        _mouseHook.Start();

        _killTimer.Tick += (_, _) => SystemServices.KillSuspendedProcesses(_cfg.KillProcesses);
        SetupKillTimer();

        _dutyTimer.Interval = TimeSpan.FromSeconds(10);
        _dutyTimer.Tick += (_, _) => CheckDutyTime();
        _dutyTimer.Start();
        InitDutyReminderState();

        MouseRightButtonUp += OnRightClick;
    }

    private void LoadBallImage()
    {
        try
        {
            // 开发者模式自定义图标优先；未设置或文件失效时回退内置 ball.png
            var iconPath = _cfg.BallIcon;
            if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
            {
                var custom = new BitmapImage();
                custom.BeginInit();
                custom.UriSource = new Uri(iconPath, UriKind.Absolute);
                custom.CacheOption = BitmapCacheOption.OnLoad;
                custom.EndInit();
                BallImage.Source = custom;
                BallFallback.Visibility = Visibility.Collapsed;
                return;
            }

            var uri = new Uri("pack://application:,,,/ball.png", UriKind.Absolute);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            BallImage.Source = bmp;
            BallFallback.Visibility = Visibility.Collapsed;
        }
        catch
        {
            BallImage.Source = null;
            BallFallback.Visibility = Visibility.Visible;
        }
    }

    private void ApplyBallSize()
    {
        var size = _cfg.BallSizePx >= 40 && _cfg.BallSizePx <= 100
            ? _cfg.BallSizePx
            : SizeMap.TryGetValue(_cfg.BallSize, out var s) ? s : 64;
        var wa = WorkArea;
        // 记录当前中心，改大小后保持中心不变，再夹进屏幕内，防止贴边越界
        double cx = Left + Width / 2.0;
        double cy = Top + Height / 2.0;
        Width = size;
        Height = size;
        Left = Math.Max(wa.Left, Math.Min(cx - size / 2.0, wa.Right - size));
        Top = Math.Max(wa.Top, Math.Min(cy - size / 2.0, wa.Bottom - size));
    }

    private Rect WorkArea => SystemParameters.WorkArea;

    private void PlaceInitial()
    {
        var wa = WorkArea;
        Left = wa.Right - Width - 6;
        Top = wa.Top + (wa.Height - Height) / 2.0;
    }

    // ---------------- 拖拽 / 点击 / 吸附 ----------------

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var screen = PointToScreen(e.GetPosition(this));
        _pressPos = screen;
        _dragOffset = new Point(screen.X - Left, screen.Y - Top);
        _moved = false;
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragOffset == null || e.LeftButton != MouseButtonState.Pressed) return;

        var screen = PointToScreen(e.GetPosition(this));
        if (_pressPos.HasValue &&
            Math.Abs(screen.X - _pressPos.Value.X) + Math.Abs(screen.Y - _pressPos.Value.Y) > 4)
        {
            _moved = true;
            HideAll();
        }

        if (_dragOffset.HasValue)
        {
            Left = screen.X - _dragOffset.Value.X;
            Top = screen.Y - _dragOffset.Value.Y;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();
        if (!_moved)
        {
            TogglePanel();
        }
        else
        {
            SnapToEdge();
        }
        _dragOffset = null;
        _pressPos = null;
        _moved = false;
    }

    private void SnapToEdge()
    {
        var wa = WorkArea;
        double centerX = Left + Width / 2.0;
        double x = centerX < wa.Left + wa.Width / 2.0 ? wa.Left + 4 : wa.Right - Width - 4;
        double y = Math.Max(wa.Top, Math.Min(Top, wa.Bottom - Height));
        Left = x;
        Top = y;
    }

    private void OnRightClick(object sender, MouseButtonEventArgs e)
    {
        var menu = new ContextMenu();
        var item = new MenuItem { Header = "设置" };
        item.Click += (_, _) => OpenSettings();
        menu.Items.Add(item);
        menu.PlacementTarget = this;
        menu.IsOpen = true;
    }

    // ---------------- 面板显示 / 隐藏 ----------------

    private void WirePanel()
    {
        _panel!.BtnTime.Click += (_, _) => ToggleTime();
        _panel.BtnServer.Click += (_, _) => OpenServer();
        _panel.BtnAi.Click += (_, _) => OpenAi();
        _panel.BtnVolume.Click += (_, _) => ToggleVolume();
        _panel.BtnRoll.Click += (_, _) => OpenRollCall();
        _panel.BtnSettings.Click += (_, _) => OpenSettings();
    }

    public void TogglePanel()
    {
        if (_panel!.IsVisible)
        {
            HideAll();
        }
        else
        {
            ShowPanel();
        }
    }

    private void ShowPanel()
    {
        _panel!.Show();
        PositionPanel();
        _panel.Activate();
        // 布局完成后按实际尺寸重新定位一次，避免越界
        Dispatcher.BeginInvoke(new Action(() => PositionPanel()), DispatcherPriority.Loaded);
        // 窗口打开 5 秒后再次置顶（对齐 Python）
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (s, _) => { ((DispatcherTimer)s!).Stop(); if (_panel!.IsVisible) _panel.Activate(); };
        timer.Start();
    }

    private static double Measure(Window w) => w.ActualWidth > 0 ? w.ActualWidth : (double.IsNaN(w.Width) ? 320 : w.Width);
    private static double MeasureH(Window w) => w.ActualHeight > 0 ? w.ActualHeight : (double.IsNaN(w.Height) ? 120 : w.Height);

    private void PositionPanel()
    {
        var wa = WorkArea;
        double pw = Measure(_panel!);
        double ph = MeasureH(_panel!);

        double x;
        if (Left + Width / 2.0 < wa.Left + wa.Width / 2.0)
            x = Left + Width + 8;
        else
            x = Left - pw - 8;

        double y = Top + Height / 2.0 - ph / 2.0;
        y = Math.Max(wa.Top + 4, Math.Min(y, wa.Bottom - ph - 4));
        x = Math.Max(wa.Left + 4, Math.Min(x, wa.Right - pw - 4));

        _panel!.Left = x;
        _panel!.Top = y;
    }

    public void HideAll()
    {
        if (_panel != null) _panel.Hide();
        if (_timeSub != null) _timeSub.Hide();
        if (_volSub != null) _volSub.Hide();
    }

    private void ToggleTime()
    {
        if (_timeSub!.IsVisible) { _timeSub.Hide(); return; }
        _volSub!.Hide();
        _timeSub.Show();
        Dispatcher.BeginInvoke(new Action(() => PlaceBelow(_panel!.BtnTime, _timeSub, 6, 12)), DispatcherPriority.Loaded);
        _timeSub.Activate();
    }

    private void ToggleVolume()
    {
        if (_volSub!.IsVisible) { _volSub.Hide(); return; }
        _timeSub!.Hide();
        _volSub.Show();
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (IsOnRightHalf())
                PlaceLeftOf(_panel!.BtnVolume, _volSub, 8);
            else
                PlaceBelow(_panel!.BtnVolume, _volSub, 6, 12);
        }), DispatcherPriority.Loaded);
        _volSub.Activate();
    }

    private bool IsOnRightHalf()
    {
        var wa = WorkArea;
        return Left + Width / 2.0 >= wa.Left + wa.Width / 2.0;
    }

    private void PlaceBelow(System.Windows.FrameworkElement anchor, Window sub, double dx, double dy)
    {
        var wa = WorkArea;
        var pt = anchor.PointToScreen(new Point(0, anchor.ActualHeight));
        double sw = Measure(sub);
        double sh = MeasureH(sub);
        double x = pt.X + dx;
        double y = pt.Y + dy;
        x = Math.Max(wa.Left + 4, Math.Min(x, wa.Right - sw - 4));
        y = Math.Max(wa.Top + 4, Math.Min(y, wa.Bottom - sh - 4));
        sub.Left = x;
        sub.Top = y;
        sub.Activate();
    }

    private void PlaceLeftOf(System.Windows.FrameworkElement anchor, Window sub, double gap)
    {
        var wa = WorkArea;
        var pt = anchor.PointToScreen(new Point(0, anchor.ActualHeight / 2.0));
        double sw = Measure(sub);
        double sh = MeasureH(sub);
        double x = pt.X - sw - gap;
        double y = pt.Y - sh / 2.0;
        x = Math.Max(wa.Left + 4, Math.Min(x, wa.Right - sw - 4));
        y = Math.Max(wa.Top + 4, Math.Min(y, wa.Bottom - sh - 4));
        sub.Left = x;
        sub.Top = y;
        sub.Activate();
    }

    private void OpenClock()
    {
        HideAll();
        if (_clockWin == null || !_clockWin.IsLoaded)
        {
            _clockWin = new FullscreenClockWindow(_cfg.EffectiveTheme) { Owner = this };
            _clockWin.Closed += (_, _) => _clockWin = null;
        }
        _clockWin.Show();
        _clockWin.Activate();
    }

    private void OpenCountdown()
    {
        HideAll();
        if (_countWin == null || !_countWin.IsLoaded)
        {
            _countWin = new CountdownWindow(_cfg.CountdownInitial) { Owner = this };
            _countWin.Closed += (_, _) => _countWin = null;
        }
        _countWin.Show();
        _countWin.Activate();
    }

    private void OpenServer()
    {
        HideAll();
        OpenUrl("http://10.207.33.240");
    }

    private void OpenAi()
    {
        HideAll();
        OpenUrl("http://8.138.105.119:8088/");
    }

    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
        catch { /* 忽略 */ }
    }

    private void OpenRollCall()
    {
        HideAll();
        new RollCallDialog(_cfg.Students, _cfg.ClassName).ShowDialog();
    }
    private void OpenSettings()
    {
        HideAll();
        new SettingsDialog(this).ShowDialog();
    }

    /// <summary>设置保存后即时应用：球大小 + 杀进程频率。</summary>
    public void ApplySettings()
    {
        ApplyBallSize();
        LoadBallImage();
        SetupKillTimer();
    }

    // ---------------- 定时器 ----------------

    private void SetupKillTimer()
    {
        _killTimer.Stop();
        switch (_cfg.KillFreq)
        {
            case "5s": _killTimer.Interval = TimeSpan.FromSeconds(5); _killTimer.Start(); break;
            case "30s": _killTimer.Interval = TimeSpan.FromSeconds(30); _killTimer.Start(); break;
            case "boot":
                var oneShot = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                oneShot.Tick += (s, _) => { ((DispatcherTimer)s!).Stop(); SystemServices.KillSuspendedProcesses(_cfg.KillProcesses); };
                oneShot.Start();
                break;
            case "custom":
                var sec = Math.Max(1, Math.Min(3600, _cfg.KillIntervalSeconds));
                _killTimer.Interval = TimeSpan.FromSeconds(sec);
                _killTimer.Start();
                break;
            default: _killTimer.Interval = TimeSpan.FromSeconds(10); _killTimer.Start(); break;
        }
    }

    private void InitDutyReminderState()
    {
        var now = DateTime.Now;
        var today = now.Date;
        int cur = now.Hour * 60 + now.Minute;

        _lastNoonReminded = Passed(_cfg.NoonDutyTime, cur) ? today : (DateTime?)null;
        _lastEveningReminded = Passed(_cfg.EveningDutyTime, cur) ? today : (DateTime?)null;
    }

    private static bool Passed(string timeStr, int currentMinutes)
    {
        try
        {
            var parts = timeStr.Split(':');
            int h = int.Parse(parts[0]);
            int m = int.Parse(parts[1]);
            return currentMinutes >= h * 60 + m;
        }
        catch { return false; }
    }

    private void CheckDutyTime()
    {
        if (!_cfg.DutyEnabled || _dutyWindowShowing) return;

        var now = DateTime.Now;
        var today = now.Date;
        int cur = now.Hour * 60 + now.Minute;

        if (TryParse(_cfg.NoonDutyTime, out var noon) && cur >= noon && _lastNoonReminded != today)
        {
            _lastNoonReminded = today;
            ShowDutyReminder("noon");
            return;
        }

        if (TryParse(_cfg.EveningDutyTime, out var evening) && cur >= evening && _lastEveningReminded != today)
        {
            _lastEveningReminded = today;
            ShowDutyReminder("evening");
        }
    }

    private static bool TryParse(string timeStr, out int minutes)
    {
        minutes = 0;
        try
        {
            var parts = timeStr.Split(':');
            minutes = int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
            return true;
        }
        catch { return false; }
    }

    private void ShowDutyReminder(string dutyType)
    {
        if (_dutyWindowShowing) return;
        try
        {
            _dutyWindowShowing = true;
            var todayKey = GetTodayDutyKey();
            if (!_cfg.DutyData.TryGetValue(todayKey, out var list) || list == null || list.Count == 0)
                return;
            var win = new DutyReminderWindow(list, dutyType) { Owner = this };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("值日提醒出错: " + ex.Message);
        }
        finally
        {
            _dutyWindowShowing = false;
        }
    }

    private string GetTodayDutyKey()
    {
        var key = DateTime.Now.DayOfWeek switch
        {
            DayOfWeek.Monday => "mon",
            DayOfWeek.Tuesday => "tue",
            DayOfWeek.Wednesday => "wed",
            DayOfWeek.Thursday => "thu",
            DayOfWeek.Friday => "fri",
            DayOfWeek.Saturday => "sat",
            DayOfWeek.Sunday => "sun",
            _ => "mon",
        };
        // 周五和周日共用：周日无数据时回退到周五
        if (key == "sun" && !_cfg.DutyData.ContainsKey("sun") && _cfg.DutyData.ContainsKey("fri"))
            return "fri";
        return key;
    }
}
