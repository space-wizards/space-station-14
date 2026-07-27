// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.DeadSpace.Blink;

public sealed class BlinkBloodTrailOverlay : Overlay
{
    private const float PointLifetime = 0.6f;

    private readonly List<ActiveTrail> _active = new();
    private readonly List<TrailPoint> _points = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public void Start(EntityUid user, TimeSpan duration)
    {
        _active.Add(new ActiveTrail(user, (float) duration.TotalSeconds));
    }

    public void Update(IEntityManager entities, float frameTime)
    {
        var transform = entities.System<SharedTransformSystem>();

        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var trail = _active[i];
            trail.Remaining -= frameTime;
            trail.SampleTimer -= frameTime;

            if (trail.Remaining <= 0f ||
                !entities.TryGetComponent<TransformComponent>(trail.User, out var xform))
            {
                _active.RemoveAt(i);
                continue;
            }

            if (trail.SampleTimer <= 0f)
            {
                trail.SampleTimer = 0.025f;
                _points.Add(new TrailPoint(xform.MapID, transform.GetWorldPosition(xform), PointLifetime));
            }

            _active[i] = trail;
        }

        for (var i = _points.Count - 1; i >= 0; i--)
        {
            var point = _points[i];
            point.Remaining -= frameTime;
            if (point.Remaining <= 0f)
                _points.RemoveAt(i);
            else
                _points[i] = point;
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        for (var i = 0; i < _points.Count; i++)
        {
            var point = _points[i];
            if (point.MapId != args.MapId)
                continue;

            var fade = Math.Clamp(point.Remaining / PointLifetime, 0f, 1f);
            var phase = i * 2.399963f;
            var offset = new Vector2(MathF.Cos(phase), MathF.Sin(phase)) * 0.09f;
            var color = new Color(0.72f, 0.015f, 0.025f, fade * 0.82f);
            var darkColor = new Color(0.28f, 0.005f, 0.008f, fade * 0.72f);

            args.WorldHandle.DrawCircle(point.Position, 0.18f, darkColor);
            args.WorldHandle.DrawCircle(point.Position + offset, 0.12f, color);
            args.WorldHandle.DrawCircle(point.Position - offset * 1.4f, 0.065f, color);
        }
    }

    private struct ActiveTrail(EntityUid user, float remaining)
    {
        public EntityUid User = user;
        public float Remaining = remaining;
        public float SampleTimer;
    }

    private struct TrailPoint(MapId mapId, Vector2 position, float remaining)
    {
        public MapId MapId = mapId;
        public Vector2 Position = position;
        public float Remaining = remaining;
    }
}
