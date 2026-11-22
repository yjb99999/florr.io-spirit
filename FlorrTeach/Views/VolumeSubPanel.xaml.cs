using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FlorrTeach.Services;

namespace FlorrTeach.Views;

public partial class VolumeSubPanel : Window
{
    private readonly VolumeController _vol = new();
    private bool _dragging;
    private readonly DispatcherTimer _syncTimer = new();

    public VolumeSubPanel()
    {
        InitializeComponent();
        WindowExtras.MarkAsToolWindow(this);

        VolumeSlider.Value = _vol.Get();
        PctText.Text = $"{VolumeSlider.Value}%";
        UpdateMuteIcon();

        VolumeSlider.PreviewMouseLeftButtonDown += (_, _) => _dragging = true;
        VolumeSlider.PreviewMouseLeftButtonUp += (_, _) => _dragging = false;
        VolumeSlider.ValueChanged += (_, _) =>
        {
            if (!_dragging) _vol.Set((int)VolumeSlider.Value);
            else _vol.Set((int)VolumeSlider.Value);
            PctText.Text = $"{(int)VolumeSlider.Value}%";
            UpdateMuteIcon();
        };

        BtnMute.Click += (_, _) =>
        {
            _vol.SetMute(!_vol.IsMuted());
            RefreshFromSystem();
        };

        _syncTimer.Interval = TimeSpan.FromMilliseconds(500);
        _syncTimer.Tick += (_, _) => RefreshFromSystem();
        _syncTimer.Start();
    }

    private void RefreshFromSystem()
    {
        if (_dragging) return;
        var v = _vol.Get();
        if ((int)VolumeSlider.Value != v) VolumeSlider.Value = v;
        PctText.Text = $"{v}%";
        UpdateMuteIcon();
    }

    private void UpdateMuteIcon()
    {
        BtnMute.Content = _vol.IsMuted() ? "🔇" : "🔊";
    }
}
