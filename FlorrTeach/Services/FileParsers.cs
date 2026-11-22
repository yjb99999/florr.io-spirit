using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FlorrTeach.Models;

namespace FlorrTeach.Services;

public static class FileParsers
{
    static FileParsers()
    {
        // .NET Core 默认不含 GBK 编码，需注册 CodePages 提供程序
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>按 UTF-8(含BOM) → GBK 顺序尝试读取文本。</summary>
    private static string ReadWithEncoding(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        // 1) UTF-8（自动识别 BOM）
        try
        {
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return utf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            // 落到 GBK
        }

        try
        {
            var gbk = Encoding.GetEncoding("GBK");
            return gbk.GetString(bytes);
        }
        catch
        {
            throw new InvalidDataException("无法识别文件编码，请使用 UTF-8 或 GBK");
        }
    }

    /// <summary>
    /// 解析名单 name.txt：第一行班级名，后面每行一个名字。
    /// </summary>
    public static (string ClassName, List<string> Students) ParseStudentsFile(string path)
    {
        var text = ReadWithEncoding(path);
        var lines = new List<string>();
        using (var reader = new StringReader(text))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var t = line.Trim();
                if (!string.IsNullOrEmpty(t)) lines.Add(t);
            }
        }

        if (lines.Count == 0) return ("", new List<string>());

        var className = lines[0];
        var students = lines.GetRange(1, lines.Count - 1);
        return (className, students);
    }

    private static readonly Dictionary<string, string> WeekdayMap = new()
    {
        ["周一"] = "mon", ["星期一"] = "mon",
        ["周二"] = "tue", ["星期二"] = "tue",
        ["周三"] = "wed", ["星期三"] = "wed",
        ["周四"] = "thu", ["星期四"] = "thu",
        ["周五"] = "fri", ["星期五"] = "fri",
        ["周六"] = "sat", ["星期六"] = "sat",
        ["周日"] = "sun", ["星期日"] = "sun",
    };

    /// <summary>
    /// 解析值日表 clean.txt：星期几开头，下面每行「名字:值务」；周五与周日共用。
    /// </summary>
    public static Dictionary<string, List<DutyItem>> ParseDutyFile(string path)
    {
        var text = ReadWithEncoding(path);
        var duty = new Dictionary<string, List<DutyItem>>();
        string? currentDay = null;

        using (var reader = new StringReader(text))
        {
            string? raw;
            while ((raw = reader.ReadLine()) != null)
            {
                var s = raw.Trim();
                if (string.IsNullOrEmpty(s)) continue;

                string? matchedDay = null;
                foreach (var kv in WeekdayMap)
                {
                    if (s == kv.Key || s.StartsWith(kv.Key, StringComparison.Ordinal))
                    {
                        matchedDay = kv.Value;
                        break;
                    }
                }

                if (matchedDay != null)
                {
                    currentDay = matchedDay;
                    if (!duty.ContainsKey(currentDay)) duty[currentDay] = new List<DutyItem>();
                    continue;
                }

                if (currentDay != null && s.Contains(':'))
                {
                    var parts = s.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var name = parts[0].Trim();
                        var role = parts[1].Trim();
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(role))
                        {
                            duty[currentDay].Add(new DutyItem(name, role));
                        }
                    }
                }
            }
        }

        // 周五和周日共用
        if (duty.TryGetValue("fri", out var fri) && !duty.ContainsKey("sun"))
            duty["sun"] = fri;
        else if (duty.TryGetValue("sun", out var sun) && !duty.ContainsKey("fri"))
            duty["fri"] = sun;

        return duty;
    }
}
