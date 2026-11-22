using System;
using System.Runtime.InteropServices;

namespace FlorrTeach.Services;

/// <summary>
/// 系统主音量控制。优先走 Core Audio (IAudioEndpointVolume)，与任务栏音量、静音实时同步；
/// Core Audio 不可用时回退 winmm (WAVE_MAPPER)。
/// </summary>
public class VolumeController
{
    private readonly CoreAudioController _core = new();

    [DllImport("winmm.dll")]
    private static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

    [DllImport("winmm.dll")]
    private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

    private int _lastVolumeBeforeMute = 50;

    /// <summary>获取主音量 0..100。</summary>
    public int Get()
    {
        if (_core.Available)
        {
            var core = _core.Get();
            if (core >= 0) return core;
        }
        try
        {
            if (waveOutGetVolume(IntPtr.Zero, out uint vol) == 0)
            {
                int left = (ushort)(vol & 0xFFFF);
                return (int)Math.Round(left / 65535.0 * 100.0);
            }
        }
        catch { /* 忽略 */ }
        return 50;
    }

    /// <summary>设置主音量 0..100。</summary>
    public void Set(int pct)
    {
        pct = Math.Max(0, Math.Min(100, pct));
        if (pct > 0) _lastVolumeBeforeMute = pct;
        if (_core.Available)
        {
            _core.Set(pct);
            return;
        }
        try
        {
            ushort v = (ushort)(pct / 100.0 * 65535.0);
            uint dw = ((uint)v << 16) | v;
            waveOutSetVolume(IntPtr.Zero, dw);
        }
        catch { /* 忽略 */ }
    }

    public bool IsMuted()
    {
        if (_core.Available) return _core.IsMuted();
        return Get() == 0;
    }

    public void SetMute(bool mute)
    {
        if (_core.Available)
        {
            if (!mute) _core.SetMute(false);
            else _core.SetMute(true);
            return;
        }
        try
        {
            if (mute)
            {
                _lastVolumeBeforeMute = Get();
                Set(0);
            }
            else
            {
                Set(_lastVolumeBeforeMute > 0 ? _lastVolumeBeforeMute : 50);
            }
        }
        catch { /* 忽略 */ }
    }
}
