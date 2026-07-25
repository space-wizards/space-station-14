using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.DeadSpace.Flamethrower;

public sealed class FlamethrowerOverlay : Overlay
{
    private const int MaxParticles = 2400;
    private readonly List<FlameParticle> _particles = new();
    private readonly IRobustRandom _random;
    private readonly Texture _flameTexture;
    private readonly Texture _flameTextureAlt;
    private readonly Texture _smokeTexture;
    private readonly Texture _emberTexture;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public FlamethrowerOverlay(IRobustRandom random, IResourceCache resources)
    {
        _random = random;
        var rsi = resources.GetResource<RSIResource>(
            new ResPath("/Textures/_Starfall/Particles/generic.rsi")).RSI;
        _flameTexture = GetTexture(rsi, "dot");
        _flameTextureAlt = GetTexture(rsi, "soft_dot");
        _smokeTexture = GetTexture(rsi, "soft_dot_big");
        _emberTexture = GetTexture(rsi, "curl");
    }

    public void Add(List<MapCoordinates> points)
    {
        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];
            var previous = points[Math.Max(0, i - 1)].Position;
            var next = points[Math.Min(points.Count - 1, i + 1)].Position;
            var direction = next - previous;
            if (direction.LengthSquared() > 0.001f)
                direction = Vector2.Normalize(direction);

            var perpendicular = new Vector2(-direction.Y, direction.X);
            var jitter = NextFloat(-0.08f, 0.08f);
            var position = point.Position + perpendicular * jitter;

            // Dense hot flame. Several overlapping particles per sample create a
            // continuous turbulent jet instead of a dotted line.
            for (var flameIndex = 0; flameIndex < 4; flameIndex++)
            {
                var flameOffset = perpendicular * NextFloat(-0.16f, 0.16f) +
                                  direction * NextFloat(-0.08f, 0.12f);
                Spawn(
                    point.MapId,
                    position + flameOffset,
                    direction * NextFloat(0.12f, 0.55f) +
                    perpendicular * NextFloat(-0.38f, 0.38f) +
                    new Vector2(0f, NextFloat(0.08f, 0.32f)),
                    NextFloat(0.38f, 0.58f),
                    NextFloat(0.26f, 0.44f),
                    ParticleKind.Flame);
            }

            // One smoke particle per sample keeps the smoke trail continuous.
            {
                Spawn(
                    point.MapId,
                    position,
                    direction * NextFloat(0.05f, 0.18f) +
                    perpendicular * NextFloat(-0.18f, 0.18f) +
                    new Vector2(0f, NextFloat(0.25f, 0.48f)),
                    NextFloat(0.75f, 1.2f),
                    NextFloat(0.24f, 0.38f),
                    ParticleKind.Smoke);
            }

            if (i % 4 == 0)
            {
                Spawn(
                    point.MapId,
                    position,
                    direction * NextFloat(0.8f, 1.45f) +
                    perpendicular * NextFloat(-0.65f, 0.65f) +
                    new Vector2(0f, NextFloat(0.15f, 0.55f)),
                    NextFloat(0.35f, 0.7f),
                    NextFloat(0.055f, 0.095f),
                    ParticleKind.Ember);
            }
        }

        if (_particles.Count > MaxParticles)
            _particles.RemoveRange(0, _particles.Count - MaxParticles);
    }

    public void Update(float frameTime)
    {
        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            particle.Age += frameTime;
            if (particle.Age >= particle.Lifetime)
            {
                _particles.RemoveAt(i);
                continue;
            }

            var drag = particle.Kind == ParticleKind.Ember ? 0.35f : 1.1f;
            particle.Velocity *= MathF.Max(0f, 1f - drag * frameTime);
            particle.NoisePhase += frameTime * 8f;
            particle.Position += particle.Velocity * frameTime;
            particle.Position.X += MathF.Sin(particle.NoisePhase) * frameTime * 0.09f;
            _particles[i] = particle;
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        DrawLayer(in args, ParticleKind.Smoke);
        DrawLayer(in args, ParticleKind.Flame);
        DrawLayer(in args, ParticleKind.Ember);
    }

    private void DrawLayer(in OverlayDrawArgs args, ParticleKind layer)
    {
        foreach (var particle in _particles)
        {
            if (particle.MapId != args.MapId || particle.Kind != layer)
                continue;

            var progress = Math.Clamp(particle.Age / particle.Lifetime, 0f, 1f);
            switch (particle.Kind)
            {
                case ParticleKind.Flame:
                {
                    var color = FlameColor(progress);
                    var ignition = Math.Clamp(progress / 0.1f, 0f, 1f);
                    var burnout = 1f - MathF.Pow(progress, 1.7f) * 0.62f;
                    var pulse = 0.88f + MathF.Sin(particle.NoisePhase * 2.35f) * 0.12f;
                    var size = particle.Size * ignition * burnout * pulse;
                    var rotation = MathF.Sin(particle.NoisePhase * 0.7f) * 0.18f;
                    var stretch = 1.02f + MathF.Sin(particle.NoisePhase * 1.3f) * 0.12f;
                    var flickerOffset = new Vector2(
                        MathF.Sin(particle.NoisePhase * 1.8f),
                        MathF.Cos(particle.NoisePhase * 1.15f)) * size * 0.12f;
                    var frame = ((int) (particle.NoisePhase * 1.5f) & 1) == 0
                        ? _flameTexture
                        : _flameTextureAlt;

                    // Large orange halo keeps the flame bright even in dark rooms.
                    DrawParticle(
                        args.WorldHandle,
                        _smokeTexture,
                        particle.Position + flickerOffset,
                        new Vector2(size * 1.32f, size * 1.24f),
                        rotation,
                        new Color(1f, 0.22f, 0.015f, color.A * 0.18f));
                    DrawParticle(
                        args.WorldHandle,
                        frame,
                        particle.Position + flickerOffset,
                        new Vector2(size * 1.08f, size * stretch),
                        rotation,
                        color);
                    DrawParticle(
                        args.WorldHandle,
                        _flameTexture,
                        particle.Position - flickerOffset * 0.35f,
                        new Vector2(size * 0.58f, size * stretch * 0.62f),
                        -rotation * 0.6f,
                        new Color(1f, 0.86f, 0.48f, color.A * (1f - progress) * 0.88f));
                    break;
                }
                case ParticleKind.Smoke:
                {
                    var alpha = FadeInOut(progress) * 0.24f;
                    var size = particle.Size * (0.72f + progress * 1.45f);
                    DrawParticle(
                        args.WorldHandle,
                        _smokeTexture,
                        particle.Position,
                        new Vector2(size, size * 1.08f),
                        particle.NoisePhase * 0.18f,
                        new Color(0.72f, 0.16f, 0.015f, alpha));
                    break;
                }
                case ParticleKind.Ember:
                {
                    var alpha = 1f - progress;
                    var size = particle.Size * (1f - progress * 0.55f);
                    var rotation = MathF.Atan2(particle.Velocity.Y, particle.Velocity.X) - MathF.PI / 2f;
                    DrawParticle(
                        args.WorldHandle,
                        _emberTexture,
                        particle.Position,
                        new Vector2(size * 0.55f, size * 1.8f),
                        rotation,
                        new Color(1f, 0.32f + 0.35f * (1f - progress), 0.015f, alpha));
                    break;
                }
            }
        }
    }

    private static Texture GetTexture(RSI rsi, string stateName)
    {
        if (!rsi.TryGetState(stateName, out var state))
            throw new ArgumentException($"Particle RSI does not contain state '{stateName}'.");

        return state.GetFrames(RsiDirection.South)[0];
    }

    private static void DrawParticle(
        DrawingHandleWorld handle,
        Texture texture,
        Vector2 position,
        Vector2 halfSize,
        float rotation,
        Color color)
    {
        var cos = MathF.Cos(rotation);
        var sin = MathF.Sin(rotation);
        handle.SetTransform(new Matrix3x2(cos, sin, -sin, cos, position.X, position.Y));
        handle.DrawTextureRect(
            texture,
            new Box2(-halfSize.X, -halfSize.Y, halfSize.X, halfSize.Y),
            color);
        handle.SetTransform(Matrix3x2.Identity);
    }
    private void Spawn(
        MapId mapId,
        Vector2 position,
        Vector2 velocity,
        float lifetime,
        float size,
        ParticleKind kind)
    {
        _particles.Add(new FlameParticle(
            mapId,
            position,
            velocity,
            lifetime,
            size,
            kind,
            NextFloat(0f, MathF.Tau)));
    }

    private float NextFloat(float min, float max)
    {
        return _random.NextFloat(min, max);
    }

    private static Color FlameColor(float progress)
    {
        if (progress < 0.2f)
            return Lerp(new Color(0.99f, 0.90f, 0.71f, FadeInOut(progress)), new Color(0.98f, 0.69f, 0.30f, 1f), progress / 0.2f);
        if (progress < 0.5f)
            return Lerp(new Color(0.98f, 0.69f, 0.30f, 1f), new Color(0.99f, 0.42f, 0.11f, 1f), (progress - 0.2f) / 0.3f);
        if (progress < 0.8f)
            return Lerp(new Color(0.99f, 0.42f, 0.11f, 1f), new Color(0.55f, 0.07f, 0.01f, 0.65f), (progress - 0.5f) / 0.3f);
        return Lerp(new Color(0.55f, 0.07f, 0.01f, 0.65f), new Color(0.08f, 0.01f, 0f, 0f), (progress - 0.8f) / 0.2f);
    }

    private static float FadeInOut(float progress)
    {
        if (progress < 0.08f)
            return progress / 0.08f;
        return Math.Clamp((1f - progress) / 0.3f, 0f, 1f);
    }

    private static Color Lerp(Color from, Color to, float amount)
    {
        return new Color(
            from.R + (to.R - from.R) * amount,
            from.G + (to.G - from.G) * amount,
            from.B + (to.B - from.B) * amount,
            from.A + (to.A - from.A) * amount);
    }

    private enum ParticleKind : byte
    {
        Flame,
        Smoke,
        Ember
    }

    private struct FlameParticle(
        MapId mapId,
        Vector2 position,
        Vector2 velocity,
        float lifetime,
        float size,
        ParticleKind kind,
        float noisePhase)
    {
        public MapId MapId = mapId;
        public Vector2 Position = position;
        public Vector2 Velocity = velocity;
        public float Lifetime = lifetime;
        public float Size = size;
        public ParticleKind Kind = kind;
        public float NoisePhase = noisePhase;
        public float Age;
    }
}
