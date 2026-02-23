using Godot;
using System.Collections.Generic;

namespace PictoMino.View.Effects;

/// <summary>
/// 方块放置时的缩放弹力动画效果。
/// </summary>
public partial class PlacementEffect : Node2D
{
    [Export] public Color FillColor { get; set; } = new Color(0.3f, 0.7f, 0.9f, 1f);
    [Export] public float AnimationDuration { get; set; } = 0.3f;
    [Export] public float ScaleOvershoot { get; set; } = 1.3f;

    private int _cellSize = 32;
    private List<Vector2I> _cells = new();
    private float _currentScale = 0f;
    private Vector2 _centerOffset;

    public void Play(List<Vector2I> cells, int cellSize, Color color)
    {
        _cells = cells;
        _cellSize = cellSize;
        FillColor = color;

        if (_cells.Count == 0) return;

        _centerOffset = CalculateCenter();
        _currentScale = 0f;

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Elastic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "CurrentScale", 1.0f, AnimationDuration);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    public float CurrentScale
    {
        get => _currentScale;
        set
        {
            _currentScale = value;
            QueueRedraw();
        }
    }

    private Vector2 CalculateCenter()
    {
        if (_cells.Count == 0) return Vector2.Zero;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var cell in _cells)
        {
            minX = Mathf.Min(minX, cell.X * _cellSize);
            minY = Mathf.Min(minY, cell.Y * _cellSize);
            maxX = Mathf.Max(maxX, (cell.X + 1) * _cellSize);
            maxY = Mathf.Max(maxY, (cell.Y + 1) * _cellSize);
        }

        return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
    }

    public override void _Draw()
    {
        if (_cells.Count == 0 || _currentScale <= 0) return;

        foreach (var cell in _cells)
        {
            float x = cell.X * _cellSize;
            float y = cell.Y * _cellSize;

            Vector2 cellCenter = new(x + _cellSize / 2f, y + _cellSize / 2f);
            Vector2 scaledOffset = (cellCenter - _centerOffset) * _currentScale;
            Vector2 scaledPos = _centerOffset + scaledOffset - new Vector2(_cellSize * _currentScale / 2f, _cellSize * _currentScale / 2f);

            Rect2 rect = new(scaledPos, new Vector2(_cellSize * _currentScale - 2, _cellSize * _currentScale - 2));
            DrawRect(rect, FillColor);
        }
    }
}
