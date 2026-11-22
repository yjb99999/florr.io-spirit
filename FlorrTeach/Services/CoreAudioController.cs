using System;
using System.Runtime.InteropServices;

namespace FlorrTeach.Services;

/// <summary>
/// 通过 Core Audio (IAudioEndpointVolume) 控制系统主音量，与任务栏音量、静音实时同步。
/// 参照 Python 版 VolumeController 的 COM 虚表调用方式；初始化失败则标记不可用，由上层回退 winmm。
/// </summary>
public sealed class CoreAudioController : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Guid
    {
        public int Data1;
        public short Data2;
        public short Data3;
        public byte Data40;
        public byte Data41;
        public byte Data42;
        public byte Data43;
        public byte Data44;
        public byte Data45;
        public byte Data46;
        public byte Data47;

        public Guid(string hex) : this(new System.Guid(hex)) { }

        public Guid(System.Guid g)
        {
            var b = g.ToByteArray();
            Data1 = BitConverter.ToInt32(b, 0);
            Data2 = BitConverter.ToInt16(b, 4);
            Data3 = BitConverter.ToInt16(b, 6);
            Data40 = b[8]; Data41 = b[9]; Data42 = b[10]; Data43 = b[11];
            Data44 = b[12]; Data45 = b[13]; Data46 = b[14]; Data47 = b[15];
        }
    }

    private const uint CLSCTX_ALL = 0x17;

    private static readonly Guid ClsidEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IidEnumerator = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    private static readonly Guid IidEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(ref Guid clsid, IntPtr pUnkOuter, uint clsctx,
        ref Guid iid, out IntPtr ppv);

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int HResultDelegate(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDefaultEndpointDelegate(IntPtr self, int dataFlow, int role, out IntPtr ppEndpoint);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int ActivateDelegate(IntPtr self, ref Guid iid, uint clsctx, IntPtr activationParams, out IntPtr ppv);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetScalarDelegate(IntPtr self, out float level);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetScalarDelegate(IntPtr self, float level, IntPtr ctx);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetMuteDelegate(IntPtr self, out int mute);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetMuteDelegate(IntPtr self, int mute, IntPtr ctx);

    private IntPtr _enumerator;
    private IntPtr _endpoint;
    private bool _initialized;

    public bool Available => _initialized && _endpoint != IntPtr.Zero;

    public CoreAudioController()
    {
        try
        {
            CoInitializeEx(IntPtr.Zero, 2); // COINIT_APARTMENTTHREADED
            Init();
        }
        catch
        {
            _initialized = false;
        }
    }

    private static IntPtr VtblEntry(IntPtr obj, int index)
    {
        IntPtr vtbl = Marshal.ReadIntPtr(obj);
        return Marshal.ReadIntPtr(vtbl, index * IntPtr.Size);
    }

    private static TDelegate GetDelegate<TDelegate>(IntPtr obj, int index) where TDelegate : Delegate
        => Marshal.GetDelegateForFunctionPointer<TDelegate>(VtblEntry(obj, index));

    private void Init()
    {
        var clsid = ClsidEnumerator;
        var iidEnum = IidEnumerator;
        int hr = CoCreateInstance(ref clsid, IntPtr.Zero, CLSCTX_ALL,
            ref iidEnum, out _enumerator);
        if (hr != 0 || _enumerator == IntPtr.Zero) return;

        var getEndpoint = GetDelegate<GetDefaultEndpointDelegate>(_enumerator, 4);
        hr = getEndpoint(_enumerator, 0, 0, out IntPtr device); // eRender=0, eConsole=0
        Release(_enumerator, 2);
        _enumerator = IntPtr.Zero;
        if (hr != 0 || device == IntPtr.Zero) return;

        var activate = GetDelegate<ActivateDelegate>(device, 3);
        var iidEpv = IidEndpointVolume;
        hr = activate(device, ref iidEpv, CLSCTX_ALL, IntPtr.Zero, out _endpoint);
        Release(device, 2);
        if (hr != 0 || _endpoint == IntPtr.Zero) { _endpoint = IntPtr.Zero; return; }

        _initialized = true;
    }

    private static void Release(IntPtr obj, int index)
    {
        if (obj == IntPtr.Zero) return;
        try { GetDelegate<HResultDelegate>(obj, index).Invoke(obj); } catch { }
    }

    /// <summary>获取主音量 0..100；静音时返回 0。不可用返回 -1。</summary>
    public int Get()
    {
        if (!Available) return -1;
        try
        {
            if (IsMuted()) return 0;
            var getScalar = GetDelegate<GetScalarDelegate>(_endpoint, 9);
            if (getScalar(_endpoint, out float level) == 0)
                return (int)Math.Round(level * 100.0);
        }
        catch { }
        return -1;
    }

    public void Set(int pct)
    {
        if (!Available) return;
        pct = Math.Max(0, Math.Min(100, pct));
        try
        {
            if (pct > 0) SetMute(false);
            var setScalar = GetDelegate<SetScalarDelegate>(_endpoint, 7);
            setScalar(_endpoint, pct / 100.0f, IntPtr.Zero);
        }
        catch { }
    }

    public bool IsMuted()
    {
        if (!Available) return false;
        try
        {
            var getMute = GetDelegate<GetMuteDelegate>(_endpoint, 15);
            return getMute(_endpoint, out int mute) == 0 && mute != 0;
        }
        catch { return false; }
    }

    public void SetMute(bool mute)
    {
        if (!Available) return;
        try
        {
            var setMute = GetDelegate<SetMuteDelegate>(_endpoint, 14);
            setMute(_endpoint, mute ? 1 : 0, IntPtr.Zero);
        }
        catch { }
    }

    public void Dispose()
    {
        Release(_endpoint, 2);
        _endpoint = IntPtr.Zero;
        _initialized = false;
    }
}
