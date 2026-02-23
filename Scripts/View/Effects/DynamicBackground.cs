using Godot;

namespace PictoMino.View.Effects;

/// <summary>
/// 动态背景效果。渐变色背景带有缓慢移动的光晕。
/// </summary>
public partial class DynamicBackground : ColorRect
{
    [Export] public Color TopColor { get; set; } = new Color(0.08f, 0.08f, 0.15f, 1f);
    [Export] public Color BottomColor { get; set; } = new Color(0.15f, 0.1f, 0.2f, 1f);
    [Export] public Color GlowColor { get; set; } = new Color(0.3f, 0.5f, 0.8f, 0.15f);
    [Export] public float GlowSpeed { get; set; } = 0.3f;
    [Export] public int GlowCount { get; set; } = 3;

    private Glow[] _glows = System.Array.Empty<Glow>();
    private float _time;

    private struct Glow
    {
        public Vector2 Position;
        public float Radius;
        public float Phase;
        public float Speed;
    }

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        Color = TopColor;

        _glows = new Glow[GlowCount];
        for (int i = 0; i < GlowCount; i++)
        {
            _glows[i] = new Glow
            {
                Position = new Vector2(GD.Randf(), GD.Randf()),
                Radius = GD.Randf() * 0.3f + 0.2f,
                Phase = GD.Randf() * Mathf.Tau,
                Speed = (GD.Randf() * 0.5f + 0.5f) * GlowSpeed
            };
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var size = GetViewportRect().Size;

        for (int y = 0; y < size.Y; y += 4)
        {
            float t = y / size.Y;
            Color lineColor = TopColor.Lerp(BottomColor, t);
            DrawLine(new Vector2(0, y), new Vector2(size.X, y), lineColor, 4);
        }

        foreach (var glow in _glows)
        {
            float offsetX = Mathf.Sin(_time * glow.Speed + glow.Phase) * 0.1f;
            float offsetY = Mathf.Cos(_time * glow.Speed * 0.7f + glow.Phase) * 0.1f;

            Vector2 center = new(
                (glow.Position.X + offsetX) * size.X,
                (glow.Position.Y + offsetY) * size.Y
            );

            float radius = glow.Radius * Mathf.Min(size.X, size.Y);

            for (int i = 20; i > 0; i--)
            {
                float r = radius * (i / 20f);
                float alpha = GlowColor.A * (1f - i / 20f) * 0.5f;
                Color c = GlowColor;
                c.A = alpha;
                DrawCircle(center, r, c);
            }
        }
    }
}
