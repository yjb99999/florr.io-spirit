using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FlorrTeach.Views;

/// <summary>
/// 苹果计时器风格的纵向滚轮选择器：可上下拖拽、松手惯性吸附到最近一项，
/// 选中项居中高亮，上下项随距离淡化、缩小，顶部与底部用渐变遮罩淡出。
/// 条目用 Canvas 显式定位，避免面板布局导致的条目缺失。
/// </summary>
public partial class WheelPicker : UserControl
{
    private const int ItemHeight = 40;
    private const int CenterSlot = 2; // 可视 5 项，中间为第 2 槽
    private const int ItemWidth = 72;

    private readonly TranslateTransform _translate = new();
    private readonly List<TextBlock> _blocks = new();

    private bool _dragging;
    private double _startY;
    private double _startOffset;
    private double _lastY;
    private DateTime _lastTime;
    private double _velocity;

    /// <summary>滚轮条目文本列表。</summary>
    public List<string> Items { get; set; } = new();

    /// <summary>当前选中下标（0 起）。</summary>
    public int SelectedIndex { get; private set; }

    /// <summary>普通条目颜色。</summary>
    public Brush ForegroundBrush { get; set; } = Brushes.Gray;

    /// <summary>选中条目颜色。</summary>
    public Brush HighlightBrush { get; set; } = Brushes.White;

    public event EventHandler? SelectionChanged;

    public WheelPicker()
    {
        InitializeComponent();
        ItemsCanvas.RenderTransform = _translate;

        MaskBorder.MouseLeftButtonDown += OnDown;
        MaskBorder.MouseMove += OnMove;
        MaskBorder.MouseLeftButtonUp += OnUp;
    }

    /// <summary>填充条目并定位到指定下标（不带动画）。</summary>
    public void Init(IEnumerable<string> items, int selectedIndex)
    {
        Items = new List<string>(items);
        BuildItems();
        PositionToIndex(selectedIndex, animate: false);
    }

    /// <summary>用卡片底色生成上下淡出遮罩。</summary>
    public void SetMaskBackground(Color bg)
    {
        var top = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        top.GradientStops.Add(new GradientStop(bg, 0));
        top.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
        TopMask.Fill = top;

        var bottom = new LinearGradientBrush
        {
            StartPoint = new Point(0, 1),
            EndPoint = new Point(0, 0)
        };
        bottom.GradientStops.Add(new GradientStop(bg, 0));
        bottom.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
        BottomMask.Fill = bottom;
    }

    private void BuildItems()
    {
        ItemsCanvas.Children.Clear();
        _blocks.Clear();
        var left = (Width - ItemWidth) / 2.0;
        for (var i = 0; i < Items.Count; i++)
        {
            var tb = new TextBlock
            {
                Text = Items[i],
                Width = ItemWidth,
                Height = ItemHeight,
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = ForegroundBrush,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            Canvas.SetTop(tb, i * ItemHeight);
            Canvas.SetLeft(tb, left);
            ItemsCanvas.Children.Add(tb);
            _blocks.Add(tb);
        }
    }

    private int ClampIndex(int idx) => Math.Max(0, Math.Min(Items.Count - 1, idx));

    private double ClampOffset(double offset)
    {
        var maxUp = -(Items.Count - 1 - CenterSlot) * ItemHeight;
        var maxDown = CenterSlot * ItemHeight;
        return Math.Max(maxUp, Math.Min(maxDown, offset));
    }

    private void PositionToIndex(int idx, bool animate)
    {
        idx = ClampIndex(idx);
        SelectedIndex = idx;
        var target = -(idx - CenterSlot) * ItemHeight;

        if (animate)
        {
            var anim = new DoubleAnimation(_translate.Y, target, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            // 吸附滚动过程中实时刷新明暗 / 缩放，避免定格在旧样式
            anim.CurrentTimeInvalidated += (_, _) => ApplyVisuals();
            anim.Completed += (_, _) => ApplyVisuals();
            _translate.BeginAnimation(TranslateTransform.YProperty, anim);
        }
        else
        {
            _translate.Y = target;
            ApplyVisuals();
        }
    }

    private void OnDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        MaskBorder.CaptureMouse();

        // 先取动画进行中的视觉位置，再移除动画并固化到本地，避免按下时跳回起点
        double visualY = _translate.Y;
        _translate.BeginAnimation(TranslateTransform.YProperty, null);
        _translate.Y = visualY;

        _startY = e.GetPosition(MaskBorder).Y;
        _startOffset = _translate.Y;
        _lastY = _startY;
        _lastTime = DateTime.Now;
        _velocity = 0;
    }

    private void OnMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;

        var y = e.GetPosition(MaskBorder).Y;
        var offset = _startOffset + (y - _startY);
        _translate.Y = ClampOffset(offset);

        var now = DateTime.Now;
        var dt = (now - _lastTime).TotalMilliseconds;
        if (dt > 0)
            _velocity = 0.8 * _velocity + 0.2 * ((y - _lastY) / dt);
        _lastY = y;
        _lastTime = now;

        ApplyVisuals();
    }

    private void OnUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        MaskBorder.ReleaseMouseCapture();

        // 惯性外推：按松手速度外推 60ms，最多滑过两格，避免甩到首尾被钳回 0
        var currentIdx = CenterSlot - _translate.Y / (double)ItemHeight;
        var fling = Math.Max(-2.0, Math.Min(2.0, _velocity * 60 / ItemHeight));
        var idx = (int)Math.Round(currentIdx + fling);
        PositionToIndex(idx, animate: true);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyVisuals()
    {
        var centerLine = CenterSlot * ItemHeight + ItemHeight / 2.0;
        for (var i = 0; i < _blocks.Count; i++)
        {
            var tb = _blocks[i];
            var cy = i * ItemHeight + ItemHeight / 2.0 + _translate.Y;
            var dist = Math.Abs(cy - centerLine);
            var t = dist / (double)ItemHeight;

            tb.Opacity = Math.Max(0.55, 1 - t / 3.0);
            var scale = Math.Max(0.92, 1 - t / 3.5);
            tb.RenderTransform = new ScaleTransform(scale, scale, 0.5, 0.5);

            var selected = dist < ItemHeight / 2.0;
            tb.Foreground = selected ? HighlightBrush : ForegroundBrush;
            tb.FontWeight = selected ? FontWeights.Bold : FontWeights.SemiBold;
        }
    }
}
