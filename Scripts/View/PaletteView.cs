using Godot;
using PictoMino.Core;
using System;
using System.Collections.Generic;

namespace PictoMino.View;

/// <summary>
/// 侧边栏视图。显示可用形状列表，支持选择交互。
/// </summary>
public partial class PaletteView : Control
{
    /// <summary>形状槽位之间的间距</summary>
    [Export] public int SlotSpacing { get; set; } = 10;

    /// <summary>单元格大小（像素）</summary>
    [Export] public int CellSize { get; set; } = 24;

    /// <summary>槽位背景颜色</summary>
    [Export] public Color SlotBackgroundColor { get; set; } = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    /// <summary>选中槽位边框颜色</summary>
    [Export] public Color SelectedBorderColor { get; set; } = new Color(1f, 0.8f, 0f, 1f);

    /// <summary>已使用槽位颜色</summary>
    [Export] public Color UsedSlotColor { get; set; } = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    /// <summary>形状填充颜色</summary>
    [Export] public Color ShapeFillColor { get; set; } = new Color(0.4f, 0.7f, 1f, 1f);

    /// <summary>形状边框颜色</summary>
    [Export] public Color ShapeBorderColor { get; set; } = new Color(0.2f, 0.4f, 0.6f, 1f);

    private PaletteData? _paletteData;
    private readonly List<Rect2> _slotRects = new();
    private int _hoverIndex = -1;

    /// <summary>
    /// 绑定的 PaletteData。
    /// </summary>
    public PaletteData? PaletteData
    {
        get => _paletteData;
        set
        {
            if (_paletteData != null)
            {
                _paletteData.OnSelectionChanged -= OnSelectionChanged;
                _paletteData.OnShapeUsedChanged -= OnShapeUsedChanged;
            }

            _paletteData = value;

            if (_paletteData != null)
            {
                _paletteData.OnSelectionChanged += OnSelectionChanged;
                _paletteData.OnShapeUsedChanged += OnShapeUsedChanged;
            }

            CalculateSlotRects();
            QueueRedraw();
        }
    }

    /// <summary>
    /// 当槽位被点击时触发。参数为槽位索引。
    /// </summary>
    public event Action<int>? OnSlotClicked;

    /// <summary>
    /// 当形状被选中时触发。参数为选中的形状。
    /// </summary>
    public event Action<ShapeData?>? OnShapeSelected;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _ExitTree()
    {
        if (_paletteData != null)
        {
            _paletteData.OnSelectionChanged -= OnSelectionChanged;
            _paletteData.OnShapeUsedChanged -= OnShapeUsedChanged;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            int clickedIndex = GetSlotAtPosition(mb.Position);
            if (clickedIndex >= 0 && _paletteData != null)
            {
                if (_paletteData.Select(clickedIndex))
                {
                    OnSlotClicked?.Invoke(clickedIndex);
                    OnShapeSelected?.Invoke(_paletteData.SelectedShape);
                }
                AcceptEvent();
            }
        }
        else if (@event is InputEventMouseMotion mm)
        {
            int newHover = GetSlotAtPosition(mm.Position);
            if (newHover != _hoverIndex)
            {
                _hoverIndex = newHover;
                QueueRedraw();
            }
        }
    }

    public override void _Draw()
    {
        if (_paletteData == null) return;

        for (int i = 0; i < _paletteData.Shapes.Count; i++)
        {
            DrawSlot(i);
        }
    }

    /// <summary>
    /// 获取指定位置的槽位索引。
    /// </summary>
    private int GetSlotAtPosition(Vector2 pos)
    {
        for (int i = 0; i < _slotRects.Count; i++)
        {
            if (_slotRects[i].HasPoint(pos))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 计算所有槽位的矩形区域。
    /// </summary>
    private void CalculateSlotRects()
    {
        _slotRects.Clear();
        if (_paletteData == null) return;

        float y = SlotSpacing;
        float maxWidth = 0;

        foreach (var shape in _paletteData.Shapes)
        {
            float slotWidth = shape.Cols * CellSize + SlotSpacing * 2;
            float slotHeight = shape.Rows * CellSize + SlotSpacing * 2;

            _slotRects.Add(new Rect2(SlotSpacing, y, slotWidth, slotHeight));

            maxWidth = Math.Max(maxWidth, slotWidth);
            y += slotHeight + SlotSpacing;
        }

        // 更新控件最小尺寸
        CustomMinimumSize = new Vector2(maxWidth + SlotSpacing * 2, y);
    }

    /// <summary>
    /// 绘制单个槽位。
    /// </summary>
    private void DrawSlot(int index)
    {
        if (_paletteData == null || index >= _slotRects.Count) return;

        var rect = _slotRects[index];
        var shape = _paletteData.Shapes[index];
        bool isUsed = _paletteData.IsUsed(index);
        bool isSelected = _paletteData.SelectedIndex == index;
        bool isHovered = _hoverIndex == index && !isUsed;

        // 绘制背景
        Color bgColor = isUsed ? UsedSlotColor : SlotBackgroundColor;
        if (isHovered && !isUsed)
        {
            bgColor = bgColor.Lightened(0.2f);
        }
        DrawRect(rect, bgColor);

        // 绘制形状
        Color fillColor = isUsed ? UsedSlotColor : ShapeFillColor;
        Vector2 shapeOffset = rect.Position + new Vector2(SlotSpacing, SlotSpacing);

        for (int r = 0; r < shape.Rows; r++)
        {
            for (int c = 0; c < shape.Cols; c++)
            {
                if (!shape.Matrix[r, c]) continue;

                Rect2 cellRect = new(
                    shapeOffset.X + c * CellSize,
                    shapeOffset.Y + r * CellSize,
                    CellSize - 1,
                    CellSize - 1
                );

                DrawRect(cellRect, fillColor);
                if (!isUsed)
                {
                    DrawRect(cellRect, ShapeBorderColor, false, 1);
                }
            }
        }

        // 绘制选中边框
        if (isSelected)
        {
            DrawRect(rect, SelectedBorderColor, false, 3);
        }
    }

    private void OnSelectionChanged(int oldIndex, int newIndex)
    {
        QueueRedraw();
    }

    private void OnShapeUsedChanged(int index, bool isUsed)
    {
        QueueRedraw();
    }

    /// <summary>
    /// 通过键盘选择下一个形状。
    /// </summary>
    public void SelectNext()
    {
        _paletteData?.SelectNext();
    }

    /// <summary>
    /// 通过键盘选择上一个形状。
    /// </summary>
    public void SelectPrevious()
    {
        _paletteData?.SelectPrevious();
    }
}
