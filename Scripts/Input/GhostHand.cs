using Godot;
using PictoMino.Core;
using PictoMino.View;

namespace PictoMino.Input;

/// <summary>
/// 放置状态枚举。
/// </summary>
public enum GhostPlacementState
{
    /// <summary>可以放置</summary>
    Valid,
    /// <summary>有重叠但可以放置</summary>
    Warning,
    /// <summary>超出边界，不可放置</summary>
    Invalid
}

/// <summary>
/// 幽灵手/光标预览。显示当前选中形状在棋盘上的预览位置。
/// </summary>
public partial class GhostHand : Node2D
{
    private BoardView? _boardView;

    /// <summary>预览颜色（可放置时）</summary>
    [Export] public Color ValidColor { get; set; } = new Color(0.2f, 0.8f, 0.2f, 0.5f);

    /// <summary>预览颜色（有重叠但可放置时）</summary>
    [Export] public Color WarningColor { get; set; } = new Color(0.9f, 0.6f, 0.1f, 0.6f);

    /// <summary>预览颜色（不可放置时）</summary>
    [Export] public Color InvalidColor { get; set; } = new Color(0.8f, 0.2f, 0.2f, 0.5f);

    /// <summary>光标颜色（无形状时）</summary>
    [Export] public Color CursorColor { get; set; } = new Color(1f, 1f, 1f, 0.3f);

    /// <summary>锚点标记颜色</summary>
    [Export] public Color AnchorColor { get; set; } = new Color(1f, 1f, 0f, 0.8f);

    /// <summary>警告边框颜色</summary>
    [Export] public Color WarningBorderColor { get; set; } = new Color(0.9f, 0.2f, 0.2f, 1f);

    private Vector2I? _gridPosition;
    private ShapeData? _currentShape;
    private GhostPlacementState _placementState = GhostPlacementState.Valid;

    /// <summary>
    /// 当前 Ghost 的棋盘坐标（锚点位置）。
    /// </summary>
    public Vector2I? GridPosition
    {
        get => _gridPosition;
        set
        {
            _gridPosition = value;
            UpdatePosition();
            QueueRedraw();
        }
    }

    /// <summary>
    /// 当前选中的形状。为 null 时仅显示光标。
    /// </summary>
    public ShapeData? CurrentShape
    {
        get => _currentShape;
        set
        {
            _currentShape = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// 当前放置状态。
    /// </summary>
    public GhostPlacementState PlacementState
    {
        get => _placementState;
        set
        {
            _placementState = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// 当前位置是否可以放置（兼容旧 API）。
    /// </summary>
    public bool IsValidPlacement
    {
        get => _placementState != GhostPlacementState.Invalid;
        set
        {
            // 旧 API 兼容：true = Valid, false = Invalid
            _placementState = value ? GhostPlacementState.Valid : GhostPlacementState.Invalid;
            QueueRedraw();
        }
    }

    /// <summary>
    /// 顺时针旋转当前形状。
    /// </summary>
    public void RotateClockwise()
    {
        if (_currentShape == null) return;
        _currentShape = _currentShape.RotateClockwise();
        QueueRedraw();
    }

    /// <summary>
    /// 逆时针旋转当前形状。
    /// </summary>
    public void RotateCounterClockwise()
    {
        if (_currentShape == null) return;
        _currentShape = _currentShape.RotateCounterClockwise();
        QueueRedraw();
    }

    public override void _Ready()
    {
        // GhostHand 是 BoardView 的子节点，直接获取父节点
        _boardView = GetParentOrNull<BoardView>();
        if (_boardView == null)
        {
            GD.PrintErr("GhostHand: Parent BoardView not found.");
        }
    }

    public override void _Draw()
    {
        if (_boardView == null || _gridPosition == null) return;

        int cellSize = _boardView.CellSize;

        if (_currentShape != null)
        {
            // 根据放置状态选择颜色
            Color fillColor = _placementState switch
            {
                GhostPlacementState.Valid => ValidColor,
                GhostPlacementState.Warning => WarningColor,
                GhostPlacementState.Invalid => InvalidColor,
                _ => ValidColor
            };

            var offsets = _currentShape.GetCellOffsetsFromAnchor();

            // 绘制形状预览填充
            foreach (var (deltaRow, deltaCol) in offsets)
            {
                Rect2 rect = new(deltaCol * cellSize, deltaRow * cellSize, cellSize, cellSize);
                DrawRect(rect, fillColor);
            }

            // 如果是警告状态，绘制红色边框
            if (_placementState == GhostPlacementState.Warning)
            {
                foreach (var (deltaRow, deltaCol) in offsets)
                {
                    Rect2 rect = new(deltaCol * cellSize + 2, deltaRow * cellSize + 2, cellSize - 4, cellSize - 4);
                    DrawRect(rect, WarningBorderColor, false, 2f);
                }
            }

            // 绘制锚点标记（小圆点）
            Vector2 anchorCenter = new(cellSize / 2f, cellSize / 2f);
            DrawCircle(anchorCenter, cellSize * 0.15f, AnchorColor);
        }
        else
        {
            // 仅绘制光标
            Rect2 rect = new(0, 0, cellSize, cellSize);
            DrawRect(rect, CursorColor);
        }
    }

    /// <summary>
    /// 更新 Ghost 的世界坐标位置（锚点位置）。
    /// </summary>
    private void UpdatePosition()
    {
        if (_boardView == null || _gridPosition == null)
        {
            Visible = false;
            return;
        }

        Visible = true;
        // 棋盘坐标 (col, row) -> 世界位置（锚点所在格的左上角）
        int col = _gridPosition.Value.X;
        int row = _gridPosition.Value.Y;
        int cellSize = _boardView.CellSize;
        var boardOffset = _boardView.BoardOffset;

        GlobalPosition = _boardView.GlobalPosition + boardOffset + new Vector2(col * cellSize, row * cellSize);
    }

    /// <summary>
    /// 设置 BoardView 引用。
    /// </summary>
    public void SetBoardView(BoardView boardView)
    {
        _boardView = boardView;
    }
}
