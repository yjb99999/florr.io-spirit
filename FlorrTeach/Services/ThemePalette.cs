using System.Windows.Media;

namespace FlorrTeach.Services;

/// <summary>
/// 全局主题配色板。黑底白字主题使用深色卡片 + 高饱和强调色；
/// 白底黑字主题使用浅色卡片 + 常规深蓝强调色。各弹窗在构造时读取并应用。
/// </summary>
public sealed class ThemePalette
{
    public string Theme { get; }

    /// <summary>卡片 / 窗口主体背景。</summary>
    public Brush CardBackground { get; }
    /// <summary>卡片描边。</summary>
    public Brush CardBorder { get; }
    /// <summary>主文字（标题、主数值）。</summary>
    public Brush PrimaryForeground { get; }
    /// <summary>次要文字（说明、日期）。</summary>
    public Brush SecondaryForeground { get; }
    /// <summary>高饱和强调色（按钮、高亮）。</summary>
    public Brush Accent { get; }
    /// <summary>强调色上的文字。</summary>
    public Brush AccentForeground { get; }
    /// <summary>次要按钮 / 标签底色。</summary>
    public Brush MutedBackground { get; }
    /// <summary>次要按钮文字。</summary>
    public Brush MutedForeground { get; }

    private ThemePalette(string theme)
    {
        Theme = theme == "white" ? "white" : "black";

        if (Theme == "white")
        {
            CardBackground = Brush("#FFFFFF");
            CardBorder = Brush("#E5E7EB");
            PrimaryForeground = Brush("#1A1A1A");
            SecondaryForeground = Brush("#555555");
            Accent = Brush("#0B6BCB");
            AccentForeground = Brush("#FFFFFF");
            MutedBackground = Brush("#F1F5F9");
            MutedForeground = Brush("#333333");
        }
        else
        {
            // 黑底：深炭卡片 + 亮蓝高饱和强调，辅以亮黄高亮
            CardBackground = Brush("#1C1C1E");
            CardBorder = Brush("#3A3A3E");
            PrimaryForeground = Brush("#FFFFFF");
            SecondaryForeground = Brush("#C7C7CC");
            Accent = Brush("#38BDF8");
            AccentForeground = Brush("#06222E");
            MutedBackground = Brush("#2C2C30");
            MutedForeground = Brush("#E5E5EA");
        }
    }

    public static ThemePalette FromConfig(string? theme) => new(theme ?? "black");

    private static SolidColorBrush Brush(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        return new SolidColorBrush(color);
    }
}
