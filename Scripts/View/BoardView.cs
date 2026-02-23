using Godot;
using PictoMino.Core;
using PictoMino.View.Effects;
using System.Collections.Generic;
using System.Linq;

namespace PictoMino.View;

/// <summary>
/// 棋盘视图层。订阅 BoardData 的变化事件，更新 TileMapLayer 显示。
/// </summary>
public partial class BoardView : Node2D
{
    /// <summary>TileSet 中的 Source ID</summary>
    private const int TileSourceId = 0;

    /// <summary>空格对应的 Atlas 坐标</summary>
    private static readonly Vector2I EmptyAtlasCoord = new(0, 0);

    /// <summary>填充格对应的 Atlas 坐标</summary>
    private static readonly Vector2I FilledAtlasCoord = new(1, 0);

    private TileMapLayer? _gridLayer;

    /// <summary>单元格大小（像素）</summary>
    [Export] public int CellSize { get; set; } = 32;

    /// <summary>提示数字区域的最大宽度（格子数）</summary>
    [Export] public int HintAreaWidth { get; set; } = 4;

    /// <summary>提示数字区域的最大高度（格子数）</summary>
    [Export] public int HintAreaHeight { get; set; } = 4;

    /// <summary>网格线颜色</summary>
    [Export] public Color GridLineColor { get; set; } = new Color(0.4f, 0.4f, 0.4f, 0.8f);

    /// <summary>边框颜色</summary>
    [Export] public Color BorderColor { get; set; } = new Color(0.8f, 0.8f, 0.8f, 1f);

    /// <summary>空格背景颜色</summary>
    [Export] public Color EmptyCellColor { get; set; } = new Color(0.25f, 0.25f, 0.28f, 1f);

    /// <summary>填充格颜色</summary>
    [Export] public Color FilledCellColor { get; set; } = new Color(0.3f, 0.7f, 0.9f, 1f);

    /// <summary>提示数字颜色（未完成）</summary>
    [Export] public Color HintTextColor { get; set; } = new Color(0.9f, 0.9f, 0.9f, 1f);

    /// <summary>提示数字颜色（已完成/正好填满）</summary>
    [Export] public Color HintCompletedColor { get; set; } = new Color(0.3f, 0.8f, 0.3f, 1f);

    /// <summary>提示数字颜色（填多了/错误）</summary>
    [Export] public Color HintErrorColor { get; set; } = new Color(0.9f, 0.3f, 0.3f, 1f);

    /// <summary>提示区域背景颜色</summary>
    [Export] public Color HintBackgroundColor { get; set; } = new Color(0.18f, 0.18f, 0.22f, 1f);

    /// <summary>形状边框颜色</summary>
    [Export] public Color ShapeBorderColor { get; set; } = new Color(0.9f, 0.9f, 0.9f, 0.9f);

    /// <summary>形状边框宽度</summary>
    [Export] public float ShapeBorderWidth { get; set; } = 2f;

    private BoardData? _boardData;
    private int[][]? _rowHints;
    private int[][]? _colHints;
    private readonly Dictionary<int, List<Vector2I>> _placedShapeCells = new();

    /// <summary>
    /// 棋盘内容区域相对于 BoardView 原点的偏移（为提示数字留空间）。
    /// 根据实际提示数字动态计算。
    /// </summary>
    public Vector2 BoardOffset
    {
        get
        {
            int maxRowHints = 1;
            int maxColHints = 1;
            if (_rowHints != null)
            {
                foreach (var hints in _rowHints)
                    if (hints.Length > maxRowHints) maxRowHints = hints.Length;
            }
            if (_colHints != null)
            {
                foreach (var hints in _colHints)
                    if (hints.Length > maxColHints) maxColHints = hints.Length;
            }
            float hintCellSize = CellSize * 0.7f;
            return new Vector2(maxRowHints * hintCellSize + 10, maxColHints * hintCellSize + 10);
        }
    }

    /// <summary>
    /// 绑定的棋盘数据。设置后会自动订阅事件并刷新视图。
    /// </summary>
    public BoardData? BoardData
    {
        get => _boardData;
        set
        {
            if (_boardData != null)
            {
                _boardData.OnCellChanged -= OnCellChanged;
            }

            _boardData = value;

            if (_boardData != null)
            {
                _boardData.OnCellChanged += OnCellChanged;
                _rowHints = _boardData.GetAllRowHints();
                _colHints = _boardData.GetAllColHints();
                RefreshAll();
            }
            else
            {
                _rowHints = null;
                _colHints = null;
            }
            
            QueueRedraw();
        }
    }

    public override void _Ready()
    {
        _gridLayer = GetNodeOrNull<TileMapLayer>("GridLayer");
        // 隐藏 TileMapLayer，改用自定义绘制
        if (_gridLayer != null)
        {
            _gridLayer.Visible = false;
        }
    }

    public override void _ExitTree()
    {
        // 清理事件订阅
        if (_boardData != null)
        {
            _boardData.OnCellChanged -= OnCellChanged;
        }
    }

    public override void _Draw()
    {
        if (_boardData == null) return;

        var offset = BoardOffset;
        int rows = _boardData.Rows;
        int cols = _boardData.Cols;

        // 绘制主棋盘背景和格子
        DrawBoardCells(offset, rows, cols);

        // 绘制网格线
        DrawGridLines(offset, rows, cols);

        // 绘制形状边框分隔
        DrawShapeBorders(offset, rows, cols);

        // 绘制边框
        DrawBoardBorder(offset, rows, cols);

        // 绘制提示数字
        DrawHints(offset, rows, cols);
    }

    /// <summary>
    /// 绘制提示区域背景。
    /// </summary>
    private void DrawHintBackgrounds(Vector2 offset, int rows, int cols)
    {
        // 左侧行提示区域背景
        var rowHintRect = new Rect2(0, offset.Y, offset.X, rows * CellSize);
        DrawRect(rowHintRect, HintBackgroundColor);

        // 顶部列提示区域背景
        var colHintRect = new Rect2(offset.X, 0, cols * CellSize, offset.Y);
        DrawRect(colHintRect, HintBackgroundColor);

        // 左上角空白区域
        var cornerRect = new Rect2(0, 0, offset.X, offset.Y);
        DrawRect(cornerRect, HintBackgroundColor.Darkened(0.1f));
    }

    /// <summary>
    /// 绘制棋盘格子。
    /// </summary>
    private void DrawBoardCells(Vector2 offset, int rows, int cols)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var cellRect = new Rect2(
                    offset.X + c * CellSize + 1,
                    offset.Y + r * CellSize + 1,
                    CellSize - 2,
                    CellSize - 2
                );

                int cellValue = _boardData!.GetCell(r, c);
                Color cellColor = cellValue == 0 ? EmptyCellColor : FilledCellColor;
                DrawRect(cellRect, cellColor);
            }
        }
    }

    /// <summary>
    /// 绘制网格线。
    /// </summary>
    private void DrawGridLines(Vector2 offset, int rows, int cols)
    {
        // 垂直线
        for (int c = 0; c <= cols; c++)
        {
            float x = offset.X + c * CellSize;
            float lineWidth = (c % 5 == 0) ? 2f : 1f;
            Color lineColor = (c % 5 == 0) ? BorderColor : GridLineColor;
            DrawLine(new Vector2(x, offset.Y), new Vector2(x, offset.Y + rows * CellSize), lineColor, lineWidth);
        }

        // 水平线
        for (int r = 0; r <= rows; r++)
        {
            float y = offset.Y + r * CellSize;
            float lineWidth = (r % 5 == 0) ? 2f : 1f;
            Color lineColor = (r % 5 == 0) ? BorderColor : GridLineColor;
            DrawLine(new Vector2(offset.X, y), new Vector2(offset.X + cols * CellSize, y), lineColor, lineWidth);
        }
    }

    /// <summary>
    /// 绘制棋盘边框。
    /// </summary>
    private void DrawBoardBorder(Vector2 offset, int rows, int cols)
    {
        var boardRect = new Rect2(offset.X, offset.Y, cols * CellSize, rows * CellSize);
        DrawRect(boardRect, BorderColor, false, 3f);
    }

    /// <summary>
    /// 绘制形状之间的边框分隔线。
    /// </summary>
    private void DrawShapeBorders(Vector2 offset, int rows, int cols)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int cellId = _boardData!.GetCell(r, c);
                if (cellId == 0) continue;

                float x = offset.X + c * CellSize;
                float y = offset.Y + r * CellSize;

                // 检查右边 - 与不同形状或空格相邻时绘制边框
                if (c < cols - 1)
                {
                    int rightId = _boardData.GetCell(r, c + 1);
                    if (rightId != cellId)
                    {
                        DrawLine(
                            new Vector2(x + CellSize, y + 1),
                            new Vector2(x + CellSize, y + CellSize - 1),
                            ShapeBorderColor, ShapeBorderWidth
                        );
                    }
                }
                else
                {
                    // 最右边也绘制边框
                    DrawLine(
                        new Vector2(x + CellSize - 1, y + 1),
                        new Vector2(x + CellSize - 1, y + CellSize - 1),
                        ShapeBorderColor, ShapeBorderWidth
                    );
                }

                // 检查下边
                if (r < rows - 1)
                {
                    int bottomId = _boardData.GetCell(r + 1, c);
                    if (bottomId != cellId)
                    {
                        DrawLine(
                            new Vector2(x + 1, y + CellSize),
                            new Vector2(x + CellSize - 1, y + CellSize),
                            ShapeBorderColor, ShapeBorderWidth
                        );
                    }
                }
                else
                {
                    // 最下边也绘制边框
                    DrawLine(
                        new Vector2(x + 1, y + CellSize - 1),
                        new Vector2(x + CellSize - 1, y + CellSize - 1),
                        ShapeBorderColor, ShapeBorderWidth
                    );
                }

                // 检查左边
                if (c == 0 || _boardData.GetCell(r, c - 1) != cellId)
                {
                    DrawLine(
                        new Vector2(x + 1, y + 1),
                        new Vector2(x + 1, y + CellSize - 1),
                        ShapeBorderColor, ShapeBorderWidth
                    );
                }

                // 检查上边
                if (r == 0 || _boardData.GetCell(r - 1, c) != cellId)
                {
                    DrawLine(
                        new Vector2(x + 1, y + 1),
                        new Vector2(x + CellSize - 1, y + 1),
                        ShapeBorderColor, ShapeBorderWidth
                    );
                }
            }
        }
    }

    /// <summary>
    /// 绘制提示数字。
    /// </summary>
    private void DrawHints(Vector2 offset, int rows, int cols)
    {
        if (_rowHints == null || _colHints == null) return;

        // 绘制行提示（左侧）
        for (int r = 0; r < rows; r++)
        {
            var targetHints = _rowHints[r];
            var currentHints = _boardData!.GetCurrentRowHints(r);
            var hintState = GetHintLineState(targetHints, currentHints);
            Color lineColor = GetHintColor(hintState);

            // 计算垂直居中位置：格子中心
            float cellCenterY = offset.Y + r * CellSize + CellSize / 2f;

            for (int i = 0; i < targetHints.Length; i++)
            {
                // 从右向左绘制，数字水平间距
                float x = offset.X - (targetHints.Length - i) * (CellSize * 0.7f) + CellSize * 0.35f;
                DrawHintNumber(targetHints[i], new Vector2(x, cellCenterY), lineColor);
            }
        }

        // 绘制列提示（顶部）
        for (int c = 0; c < cols; c++)
        {
            var targetHints = _colHints[c];
            var currentHints = _boardData!.GetCurrentColHints(c);
            var hintState = GetHintLineState(targetHints, currentHints);
            Color lineColor = GetHintColor(hintState);

            // 计算水平居中位置：格子中心
            float cellCenterX = offset.X + c * CellSize + CellSize / 2f;

            for (int i = 0; i < targetHints.Length; i++)
            {
                // 从下向上绘制
                float y = offset.Y - (targetHints.Length - i) * (CellSize * 0.7f) + CellSize * 0.35f;
                DrawHintNumber(targetHints[i], new Vector2(cellCenterX, y), lineColor);
            }
        }
    }

    /// <summary>
    /// 提示行/列的状态枚举。
    /// </summary>
    private enum HintState
    {
        Incomplete,  // 未完成
        Complete,    // 正好完成
        Error        // 填多了
    }

    /// <summary>
    /// 比较目标提示和当前提示，返回状态。
    /// </summary>
    private static HintState GetHintLineState(int[] target, int[] current)
    {
        int targetSum = target.Sum();
        int currentSum = current.Sum();

        if (currentSum > targetSum)
        {
            return HintState.Error;
        }

        // 检查是否完全匹配
        if (target.Length == current.Length)
        {
            bool allMatch = true;
            for (int i = 0; i < target.Length; i++)
            {
                if (target[i] != current[i])
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch) return HintState.Complete;
        }

        return HintState.Incomplete;
    }

    /// <summary>
    /// 根据状态返回颜色。
    /// </summary>
    private Color GetHintColor(HintState state)
    {
        return state switch
        {
            HintState.Complete => HintCompletedColor,
            HintState.Error => HintErrorColor,
            _ => HintTextColor
        };
    }

    /// <summary>
    /// 绘制单个提示数字。
    /// </summary>
    private void DrawHintNumber(int number, Vector2 center, Color color)
    {
        string text = number.ToString();
        var font = ThemeDB.FallbackFont;
        int fontSize = (int)(CellSize * 0.5f);
        var textSize = font.GetStringSize(text, HorizontalAlignment.Center, -1, fontSize);
        // 文字位置：水平居中，垂直居中（调整基线）
        var pos = center - new Vector2(textSize.X / 2f, -textSize.Y * 0.3f);
        DrawString(font, pos, text, HorizontalAlignment.Left, -1, fontSize, color);
    }

    /// <summary>
    /// 将棋盘坐标转换为世界坐标（格子中心）。
    /// </summary>
    public Vector2 GridToWorld(int row, int col)
    {
        var offset = BoardOffset;
        return GlobalPosition + offset + new Vector2(col * CellSize + CellSize / 2f, row * CellSize + CellSize / 2f);
    }

    /// <summary>
    /// 将世界坐标转换为棋盘坐标。
    /// </summary>
    public Vector2I WorldToGrid(Vector2 worldPos)
    {
        var offset = BoardOffset;
        Vector2 localPos = worldPos - GlobalPosition - offset;
        int col = Mathf.FloorToInt(localPos.X / CellSize);
        int row = Mathf.FloorToInt(localPos.Y / CellSize);
        return new Vector2I(col, row); // 返回 (col, row) 以便于输入处理
    }

    /// <summary>
    /// 将棋盘坐标转换为本地坐标（TileMapLayer 坐标系）。
    /// </summary>
    public Vector2I GridToTileCoord(int row, int col)
    {
        // TileMapLayer 使用 (x, y) 即 (col, row)
        return new Vector2I(col, row);
    }

    /// <summary>
    /// 刷新所有格子的显示。
    /// </summary>
    public void RefreshAll()
    {
        QueueRedraw();
    }

    /// <summary>
    /// 当单元格状态变化时的回调。
    /// </summary>
    private void OnCellChanged(int row, int col, int newValue)
    {
        if (newValue > 0)
        {
            if (!_placedShapeCells.ContainsKey(newValue))
            {
                _placedShapeCells[newValue] = new List<Vector2I>();
            }
            _placedShapeCells[newValue].Add(new Vector2I(col, row));
        }
        QueueRedraw();
    }

    /// <summary>
    /// 播放形状放置动画。
    /// </summary>
    public void PlayPlacementEffect(int shapeId)
    {
        if (!_placedShapeCells.TryGetValue(shapeId, out var cells) || cells.Count == 0)
            return;

        var effect = new PlacementEffect();
        AddChild(effect);
        effect.Position = BoardOffset;
        effect.Play(new List<Vector2I>(cells), CellSize, FilledCellColor);

        _placedShapeCells.Remove(shapeId);
    }

    /// <summary>
    /// 清除形状追踪数据。
    /// </summary>
    public void ClearShapeTracking()
    {
        _placedShapeCells.Clear();
    }

    /// <summary>
    /// 更新单个格子的 Tile 显示。
    /// </summary>
    private void UpdateTile(int row, int col, int value)
    {
        QueueRedraw();
    }
}
