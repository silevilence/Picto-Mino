using Godot;

namespace PictoMino.View.Effects;

/// <summary>
/// 胜利时的粒子特效。
/// </summary>
public partial class WinParticleEffect : Node2D
{
    [Export] public int ParticleCount { get; set; } = 50;
    [Export] public float Duration { get; set; } = 2.0f;
    [Export] public float SpreadRadius { get; set; } = 300f;
    [Export] public Color[] ParticleColors { get; set; } = new Color[]
    {
        new(1f, 0.8f, 0.2f, 1f),
        new(0.2f, 0.8f, 1f, 1f),
        new(1f, 0.4f, 0.6f, 1f),
        new(0.4f, 1f, 0.6f, 1f),
        new(0.8f, 0.4f, 1f, 1f)
    };

    private Particle[] _particles = System.Array.Empty<Particle>();
    private float _elapsed;
    private bool _playing;

    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Size;
        public float Rotation;
        public float RotationSpeed;
        public float Lifetime;
    }

    public void Play(Vector2 center)
    {
        GlobalPosition = center;
        _particles = new Particle[ParticleCount];
        _elapsed = 0f;
        _playing = true;

        for (int i = 0; i < ParticleCount; i++)
        {
            float angle = GD.Randf() * Mathf.Tau;
            float speed = GD.Randf() * 200f + 100f;

            _particles[i] = new Particle
            {
                Position = Vector2.Zero,
                Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                Color = ParticleColors[GD.Randi() % ParticleColors.Length],
                Size = GD.Randf() * 8f + 4f,
                Rotation = GD.Randf() * Mathf.Tau,
                RotationSpeed = (GD.Randf() - 0.5f) * 10f,
                Lifetime = GD.Randf() * 0.5f + 0.5f
            };
        }

        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!_playing) return;

        _elapsed += (float)delta;

        for (int i = 0; i < _particles.Length; i++)
        {
            ref var p = ref _particles[i];
            p.Position += p.Velocity * (float)delta;
            p.Velocity.Y += 300f * (float)delta;
            p.Velocity *= 0.98f;
            p.Rotation += p.RotationSpeed * (float)delta;
        }

        QueueRedraw();

        if (_elapsed >= Duration)
        {
            _playing = false;
            QueueFree();
        }
    }

    public override void _Draw()
    {
        if (!_playing) return;

        float progress = _elapsed / Duration;

        for (int i = 0; i < _particles.Length; i++)
        {
            ref var p = ref _particles[i];
            float particleProgress = Mathf.Clamp(_elapsed / (Duration * p.Lifetime), 0f, 1f);
            float alpha = 1f - particleProgress;

            if (alpha <= 0) continue;

            Color color = p.Color;
            color.A = alpha;

            Vector2[] points = new Vector2[4];
            float halfSize = p.Size / 2f;
            for (int j = 0; j < 4; j++)
            {
                float cornerAngle = p.Rotation + j * Mathf.Pi / 2f + Mathf.Pi / 4f;
                points[j] = p.Position + new Vector2(Mathf.Cos(cornerAngle), Mathf.Sin(cornerAngle)) * halfSize * 1.414f;
            }

            DrawColoredPolygon(points, color);
        }
    }
}
