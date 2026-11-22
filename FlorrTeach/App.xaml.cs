using System;
using System.IO;
using System.Windows;
using FlorrTeach.Services;
using FlorrTeach.Views;

namespace FlorrTeach;

public partial class App : Application
{
    public static ConfigManager Config { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SetupExceptionLog();

        // 单实例：已有实例则直接退出
        if (SystemServices.IsAlreadyRunning())
        {
            Shutdown();
            return;
        }

        SystemServices.SetAppUserModelId();
        SystemServices.EnsureAutostart();

        Config = new ConfigManager();

        // 从程序所在目录读取 name.txt / clean.txt（若存在）
        var baseDir = AppContext.BaseDirectory;
        try
        {
            var namePath = Path.Combine(baseDir, "name.txt");
            if (File.Exists(namePath))
            {
                var (className, students) = FileParsers.ParseStudentsFile(namePath);
                Config.Data.ClassName = className;
                Config.Data.Students = students;
                Config.Save();
            }

            var cleanPath = Path.Combine(baseDir, "clean.txt");
            if (File.Exists(cleanPath))
            {
                Config.Data.DutyData = FileParsers.ParseDutyFile(cleanPath);
                Config.Save();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("读取名单/值日表失败: " + ex.Message);
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var ball = new FloatingBallWindow();
        ball.Show();
    }

    private void SetupExceptionLog()
    {
        void WriteLog(string source, Exception ex)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "florr_teach");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, "crash.log");
                File.AppendAllText(file,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ({source}) {ex}\n\n");
            }
            catch { /* 忽略 */ }
        }

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex) WriteLog("AppDomain", ex);
        };

        DispatcherUnhandledException += (_, args) =>
        {
            WriteLog("Dispatcher", args.Exception);
            args.Handled = true;
        };
    }
}
