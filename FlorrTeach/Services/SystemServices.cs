using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace FlorrTeach.Services;

public static class SystemServices
{
    private const string MutexName = "Local\\huahua_teach_desktop_single_v1";
    private static Mutex? _mutex;

    /// <summary>返回 true 表示已有另一个实例在运行（应退出）。</summary>
    public static bool IsAlreadyRunning()
    {
        _mutex = new Mutex(false, MutexName, out bool createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            return true;
        }
        return false;
    }

    /// <summary>写入 HKCU Run 开机自启（当前用户，无需管理员）。</summary>
    public static void EnsureAutostart()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath)) return;
            var cmd = "\"" + exePath + "\"";

            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.SetValue("HuahuaTeachDesktop", cmd, RegistryValueKind.String);
        }
        catch
        {
            // 静默失败，不影响主程序
        }
    }

    /// <summary>按配置结束指定悬浮进程（如班级优化大师）。</summary>
    public static void KillSuspendedProcesses(System.Collections.Generic.IList<string> processNames)
    {
        if (processNames == null || processNames.Count == 0) return;
        foreach (var name in processNames)
        {
            var trimmed = name.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            try
            {
                // 先 tasklist 探测存在，再 taskkill
                var check = RunHidden("tasklist", $"/FI \"IMAGENAME eq {trimmed}\"");
                if (check != null && check.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    RunHidden("taskkill", $"/F /IM \"{trimmed}\"");
                }
            }
            catch
            {
                // 忽略单个进程异常
            }
        }
    }

    private static string? RunHidden(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(5000);
            return output;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("shell32.dll")]
    private static extern void SetCurrentProcessExplicitAppUserModelID(string appID);

    /// <summary>设置任务栏 AppUserModelID，避免图标归并到系统默认。</summary>
    public static void SetAppUserModelId()
    {
        try { SetCurrentProcessExplicitAppUserModelID("florr.teach.1.0"); }
        catch { /* 忽略 */ }
    }
}
