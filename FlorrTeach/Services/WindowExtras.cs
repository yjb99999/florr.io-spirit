using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FlorrTeach.Services;

/// <summary>
/// 窗口扩展样式辅助。将附属窗口标记为工具窗口（WS_EX_TOOLWINDOW），
/// 使其既不显示在任务栏，也不出现在 Alt+Tab / Win+Tab 切换列表中，
/// 对齐 Python 版的 Qt.Tool 窗口标志。全屏时钟例外：保持普通顶层窗口。
/// </summary>
public static class WindowExtras
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    /// <summary>在窗口句柄创建后追加工具窗口样式，不进任务栏、不进 Alt+Tab。</summary>
    public static void MarkAsToolWindow(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var ex = GetWindowLong(hwnd, GWL_EXSTYLE);
            ex |= WS_EX_TOOLWINDOW;
            ex &= ~WS_EX_APPWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, ex);
        };
    }
}
