using System;
using Godot;
using PictoMino.View;

namespace PictoMino.Input;

/// <summary>
/// 鼠标输入策略。Ghost 跟随鼠标实时移动。
/// </summary>
public class MouseStrategy : IInputStrategy
{
    private readonly BoardView _boardView;
    private readonly Node _inputNode;
    
    private Vector2I? _currentGridPos;
    private bool _isActive;

    /// <summary>
    /// 当鼠标移动到新的格子时触发。参数为新的棋盘坐标 (col, row)。
    /// </summary>
    public event Action<Vector2I>? OnGridPositionChanged;

    /// <summary>
    /// 当鼠标点击时触发。参数为点击的棋盘坐标 (col, row)。
    /// </summary>
    public event Action<Vector2I>? OnInteract;

    /// <summary>
    /// 当鼠标右键点击时触发（取消操作）。
    /// </summary>
    public event Action? OnCancel;

    /// <summary>
    /// 当顺时针旋转时触发（鼠标滚轮向上）。
    /// </summary>
    public event Action? OnRotateClockwise;

    /// <summary>
    /// 当逆时针旋转时触发（鼠标滚轮向下）。
    /// </summary>
    public event Action? OnRotateCounterClockwise;

    public MouseStrategy(BoardView boardView, Node inputNode)
    {
        _boardView = boardView ?? throw new ArgumentNullException(nameof(boardView));
        _inputNode = inputNode ?? throw new ArgumentNullException(nameof(inputNode));
    }

    public void OnActivate()
    {
        _isActive = true;
        _currentGridPos = null;
    }

    public void OnDeactivate()
    {
        _isActive = false;
        _currentGridPos = null;
    }

    public bool HandleInput(InputEvent @event)
    {
        if (!_isActive) return false;

        // 处理鼠标按钮事件
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            Vector2I gridPos = _boardView.WorldToGrid(mouseButton.GlobalPosition);

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                OnInteract?.Invoke(gridPos);
                return true;
            }
            else if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                OnCancel?.Invoke();
                return true;
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                OnRotateClockwise?.Invoke();
                return true;
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                OnRotateCounterClockwise?.Invoke();
                return true;
            }
        }

        return false;
    }

    public void Process(double delta)
    {
        if (!_isActive) return;

        // 获取鼠标位置并转换为棋盘坐标
        Vector2 mousePos = _inputNode.GetViewport().GetMousePosition();
        Vector2I newGridPos = _boardView.WorldToGrid(mousePos);

        // 检查是否移动到新格子
        if (_currentGridPos != newGridPos)
        {
            _currentGridPos = newGridPos;
            OnGridPositionChanged?.Invoke(newGridPos);
        }
    }

    public Vector2I? GetGhostGridPosition()
    {
        return _currentGridPos;
    }
}
