using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FlorrTeach.Services;

/// <summary>
/// 全局低级鼠标钩子：当鼠标左键按下命中的窗口不属于本进程时，
/// 通过 <see cref="MouseClickedOutside"/> 通知调用方收起悬浮面板。
/// 替代原 WPF Deactivated 失活收回（失活会把刚打开的子面板也连带收掉）。
/// </summary>
public sealed class GlobalMouseHook : IDisposable
{
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT pt);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint ga);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

    private const int WH_MOUSE_LL = 13;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const uint GA_ROOT = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private IntPtr _hook;
    private LowLevelMouseProc? _proc;

    /// <summary>左键命中本进程之外的窗口时触发（回调在主线程消息循环上）。</summary>
    public event Action? MouseClickedOutside;

    public void Start()
    {
        if (_hook != IntPtr.Zero) return;
        _proc = HookCallback;
        _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, IntPtr.Zero, 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (int)wParam == WM_LBUTTONDOWN)
        {
            try
            {
                var pt = Marshal.PtrToStructure<POINT>(lParam);
                var hwnd = WindowFromPoint(pt);
                if (hwnd != IntPtr.Zero)
                {
                    var root = GetAncestor(hwnd, GA_ROOT);
                    GetWindowThreadProcessId(root, out uint pid);
                    if (pid != (uint)Environment.ProcessId)
                    {
                        MouseClickedOutside?.Invoke();
                    }
                }
            }
            catch { /* 钩子回调内不得抛出 */ }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
        _proc = null;
    }
}
