using System;
using System.IO;
using System.Text.Json;
using FlorrTeach.Models;

namespace FlorrTeach.Services;

/// <summary>配置读写：位于 %APPDATA%\florr_teach\config.json，避免 Program Files 权限问题。</summary>
public class ConfigManager
{
    public AppConfig Data { get; private set; } = new();

    private readonly string _configPath;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
    };

    public ConfigManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "florr_teach");
        Directory.CreateDirectory(dir);
        _configPath = Path.Combine(dir, "config.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOpts);
                if (cfg != null)
                {
                    Data = cfg;
                    Data.Students ??= new();
                    Data.DutyData ??= new();
                    Data.KillProcesses ??= new();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("读取配置失败: " + ex.Message);
        }
        Data = new AppConfig();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Data, JsonOpts);
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("保存配置失败: " + ex.Message);
        }
    }
}
