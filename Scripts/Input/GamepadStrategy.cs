using System;
using Godot;

namespace PictoMino.Input;

/// <summary>
/// 手柄/键盘输入策略。Ghost 响应离散方向键移动。
/// </summary>
public class GamepadStrategy : IInputStrategy
{
    private readonly int _boardRows;
    private readonly int _boardCols;

    private Vector2I _cursorPos;
    private bool _isActive;

    /// <summary>
    /// 当光标移动到新的格子时触发。参数为新的棋盘坐标 (col, row)。
    /// </summary>
    public event Action<Vector2I>? OnGridPositionChanged;

    /// <summary>
    /// 当确认按钮按下时触发。参数为当前棋盘坐标 (col, row)。
    /// </summary>
    public event Action<Vector2I>? OnInteract;

    /// <summary>
    /// 当取消按钮按下时触发。
    /// </summary>
    public event Action? OnCancel;

    // Input Map 中定义的动作名称
    private const string ActionUp = "cursor_up";
    private const string ActionDown = "cursor_down";
    private const string ActionLeft = "cursor_left";
    private const string ActionRight = "cursor_right";
    private const string ActionMain = "interact_main";
    private const string ActionSecondary = "interact_secondary";

    public GamepadStrategy(int boardRows, int boardCols, Vector2I? initialPos = null)
    {
        if (boardRows <= 0) throw new ArgumentOutOfRangeException(nameof(boardRows));
        if (boardCols <= 0) throw new ArgumentOutOfRangeException(nameof(boardCols));

        _boardRows = boardRows;
        _boardCols = boardCols;
        _cursorPos = initialPos ?? Vector2I.Zero;
    }

    public void OnActivate()
    {
        _isActive = true;
    }

    public void OnDeactivate()
    {
        _isActive = false;
    }

    public bool HandleInput(InputEvent @event)
    {
        if (!_isActive) return false;

        // 只处理按下事件
        if (!@event.IsPressed() || @event.IsEcho()) return false;

        // 方向键移动
        if (@event.IsAction(ActionUp))
        {
            MoveCursor(0, -1);
            return true;
        }
        if (@event.IsAction(ActionDown))
        {
            MoveCursor(0, 1);
            return true;
        }
        if (@event.IsAction(ActionLeft))
        {
            MoveCursor(-1, 0);
            return true;
        }
        if (@event.IsAction(ActionRight))
        {
            MoveCursor(1, 0);
            return true;
        }

        // 交互按钮
        if (@event.IsAction(ActionMain))
        {
            OnInteract?.Invoke(_cursorPos);
            return true;
        }
        if (@event.IsAction(ActionSecondary))
        {
            OnCancel?.Invoke();
            return true;
        }

        return false;
    }

    public void Process(double delta)
    {
        // 手柄策略不需要每帧处理
    }

    public Vector2I? GetGhostGridPosition()
    {
        return _cursorPos;
    }

    /// <summary>
    /// 设置光标位置（边界限制）。
    /// </summary>
    public void SetCursorPosition(Vector2I pos)
    {
        int newCol = Math.Clamp(pos.X, 0, _boardCols - 1);
        int newRow = Math.Clamp(pos.Y, 0, _boardRows - 1);
        
        Vector2I newPos = new(newCol, newRow);
        if (_cursorPos != newPos)
        {
            _cursorPos = newPos;
            OnGridPositionChanged?.Invoke(_cursorPos);
        }
    }

    /// <summary>
    /// 移动光标（相对移动）。
    /// </summary>
    private void MoveCursor(int deltaCol, int deltaRow)
    {
        SetCursorPosition(new Vector2I(_cursorPos.X + deltaCol, _cursorPos.Y + deltaRow));
    }
}
