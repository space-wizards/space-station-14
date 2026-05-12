using Content.Shared.Interaction;
using Content.Shared.Wall;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using System.Numerics;

namespace Content.Client.Wall;

/// <summary>
/// Shows the area in which entities with <see cref="WallMountComponent" /> can be interacted from.
/// </summary>
public sealed class WallmountDebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly SharedTransformSystem _transform;
    private readonly EntityLookupSystem _lookup;
    private readonly HashSet<Entity<WallMountComponent>> _intersecting = [];

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public WallmountDebugOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entManager.System<SharedTransformSystem>();
        _lookup = _entManager.System<EntityLookupSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _intersecting.Clear();
        _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds, _intersecting);
        foreach (var ent in _intersecting)
        {
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(ent.Owner);
            DrawArc(args.WorldHandle, worldPos, SharedInteractionSystem.InteractionRange, worldRot + ent.Comp.Direction, ent.Comp.Arc);
        }
    }

    private static void DrawArc(DrawingHandleWorld handle, Vector2 position, float radius, Angle rot, Angle arc)
    {
        // 32 segments for a full circle, but 2 at least
        var segments = Math.Max((int)(arc.Theta / Math.Tau * 32), 2);
        var step = arc.Theta / (segments - 1);
        var verts = new Vector2[segments + 1];

        verts[0] = position;
        for (var i = 0; i < segments; i++)
        {
            var angle = (float)(-arc.Theta / 2 + i * step - rot.Theta + Math.PI);
            var pos = new Vector2(MathF.Sin(angle), MathF.Cos(angle));

            verts[i + 1] = position + pos * radius;
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, Color.Green.WithAlpha(0.5f));
    }
}
