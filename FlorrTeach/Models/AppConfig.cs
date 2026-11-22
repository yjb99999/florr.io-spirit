using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlorrTeach.Models;

/// <summary>单条值日记录：名字 + 值务</summary>
public class DutyItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    public DutyItem() { }

    public DutyItem(string name, string role)
    {
        Name = name;
        Role = role;
    }
}

/// <summary>
/// 应用配置，字段与 Python 版 config.json 保持一致（snake_case 键名）。
/// </summary>
public class AppConfig
{
    [JsonPropertyName("time_theme")]
    public string TimeTheme { get; set; } = "black"; // black / white

    [JsonPropertyName("class_name")]
    public string ClassName { get; set; } = "";

    [JsonPropertyName("students")]
    public List<string> Students { get; set; } = new();

    [JsonPropertyName("countdown_initial")]
    public int CountdownInitial { get; set; } = 300;

    [JsonPropertyName("ball_size")]
    public string BallSize { get; set; } = "medium"; // small / medium / large

    [JsonPropertyName("duty_data")]
    public Dictionary<string, List<DutyItem>> DutyData { get; set; } = new();

    [JsonPropertyName("duty_enabled")]
    public bool DutyEnabled { get; set; } = false;

    [JsonPropertyName("noon_duty_time")]
    public string NoonDutyTime { get; set; } = "12:30";

    [JsonPropertyName("evening_duty_time")]
    public string EveningDutyTime { get; set; } = "21:40";

    [JsonPropertyName("kill_processes")]
    public List<string> KillProcesses { get; set; } = new();

    [JsonPropertyName("kill_freq")]
    public string KillFreq { get; set; } = "10s"; // 5s / 10s / 30s / boot / custom

    /// <summary>自定义间隔杀进程的秒数（仅 KillFreq=custom 时生效）。</summary>
    [JsonPropertyName("kill_interval_seconds")]
    public int KillIntervalSeconds { get; set; } = 10;

    /// <summary>开发者模式是否已激活。</summary>
    [JsonPropertyName("dev_mode")]
    public bool DevMode { get; set; } = false;

    /// <summary>自定义悬浮球图标 PNG 路径；为空时使用内置 ball.png。</summary>
    [JsonPropertyName("ball_icon")]
    public string BallIcon { get; set; } = "";

    /// <summary>开发者模式独立界面主题：""=跟随全局，black / white。</summary>
    [JsonPropertyName("panel_theme")]
    public string PanelTheme { get; set; } = "";

    /// <summary>开发者模式自定义球大小（像素）；0 = 使用三档 BallSize。</summary>
    [JsonPropertyName("ball_size_px")]
    public int BallSizePx { get; set; } = 0;

    /// <summary>弹窗实际生效主题：独立主题优先，否则回退全局时钟主题。</summary>
    public string EffectiveTheme => !string.IsNullOrEmpty(PanelTheme) ? PanelTheme : TimeTheme;
}
