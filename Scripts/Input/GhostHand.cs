using Godot;
using PictoMino.Core;
using PictoMino.View;

namespace PictoMino.Input;

/// <summary>
/// 幽灵手/光标预览。显示当前选中形状在棋盘上的预览位置。
/// </summary>
public partial class GhostHand : Node2D
{
    private BoardView? _boardView;

    /// <summary>预览颜色（可放置时）</summary>
    [Export] public Color ValidColor { get; set; } = new Color(0.2f, 0.8f, 0.2f, 0.5f);

    /// <summary>预览颜色（不可放置时）</summary>
    [Export] public Color InvalidColor { get; set; } = new Color(0.8f, 0.2f, 0.2f, 0.5f);

    /// <summary>光标颜色（无形状时）</summary>
    [Export] public Color CursorColor { get; set; } = new Color(1f, 1f, 1f, 0.3f);

    private Vector2I? _gridPosition;
    private ShapeData? _currentShape;
    private bool _isValidPlacement;

    /// <summary>
    /// 当前 Ghost 的棋盘坐标。
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
    /// 当前位置是否可以放置。
    /// </summary>
    public bool IsValidPlacement
    {
        get => _isValidPlacement;
        set
        {
            _isValidPlacement = value;
            QueueRedraw();
        }
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
            // 绘制形状预览
            Color color = _isValidPlacement ? ValidColor : InvalidColor;

            for (int r = 0; r < _currentShape.Rows; r++)
            {
                for (int c = 0; c < _currentShape.Cols; c++)
                {
                    if (!_currentShape.Matrix[r, c]) continue;

                    Rect2 rect = new(c * cellSize, r * cellSize, cellSize, cellSize);
                    DrawRect(rect, color);
                }
            }
        }
        else
        {
            // 仅绘制光标
            Rect2 rect = new(0, 0, cellSize, cellSize);
            DrawRect(rect, CursorColor);
        }
    }

    /// <summary>
    /// 更新 Ghost 的世界坐标位置。
    /// </summary>
    private void UpdatePosition()
    {
        if (_boardView == null || _gridPosition == null)
        {
            Visible = false;
            return;
        }

        Visible = true;
        // 棋盘坐标 (col, row) -> 世界位置（左上角）
        int col = _gridPosition.Value.X;
        int row = _gridPosition.Value.Y;
        int cellSize = _boardView.CellSize;

        GlobalPosition = _boardView.GlobalPosition + new Vector2(col * cellSize, row * cellSize);
    }

    /// <summary>
    /// 设置 BoardView 引用。
    /// </summary>
    public void SetBoardView(BoardView boardView)
    {
        _boardView = boardView;
    }
}
