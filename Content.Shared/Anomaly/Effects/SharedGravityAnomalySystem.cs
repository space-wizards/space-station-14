using System.Linq;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Ghost;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Content.Shared.Physics;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Anomaly.Effects;

public abstract class SharedGravityAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GravityAnomalyComponent, AnomalyPulseEvent>(OnAnomalyPulse);
        SubscribeLocalEvent<GravityAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnAnomalyPulse(EntityUid uid, GravityAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var xform = Transform(uid);
        var range = component.MaxThrowRange * args.Severity * args.PowerModifier;
        var strength = component.MaxThrowStrength * args.Severity * args.PowerModifier;
        var lookup = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic | LookupFlags.Sundries);
        var xformQuery = GetEntityQuery<TransformComponent>();
        var worldPos = _xform.GetWorldPosition(xform, xformQuery);
        var physQuery = GetEntityQuery<PhysicsComponent>();

        foreach (var ent in lookup)
        {
            if (physQuery.TryGetComponent(ent, out var phys)
                && (phys.CollisionMask & (int) CollisionGroup.GhostImpassable) != 0)
                continue;

            var foo = _xform.GetWorldPosition(ent, xformQuery) - worldPos;
            _throwing.TryThrow(ent, foo * 10, strength, uid, 0);
        }
    }

    private void OnSupercritical(EntityUid uid, GravityAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        var xform = Transform(uid);
        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
            return;

        var worldPos = _xform.GetWorldPosition(xform);
        var tileref = _mapSystem.GetTilesIntersecting(
                xform.GridUid.Value,
                grid,
                new Circle(worldPos, component.SpaceRange))
            .ToArray();

        var tiles = tileref.Select(t => (t.GridIndices, Tile.Empty)).ToList();
        _mapSystem.SetTiles(xform.GridUid.Value, grid, tiles);

        var range = component.MaxThrowRange * 2 * args.PowerModifier;
        var strength = component.MaxThrowStrength * 2 * args.PowerModifier;
        var lookup = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic | LookupFlags.Sundries);
        var xformQuery = GetEntityQuery<TransformComponent>();
        var physQuery = GetEntityQuery<PhysicsComponent>();

        foreach (var ent in lookup)
        {
            if (physQuery.TryGetComponent(ent, out var phys)
                && (phys.CollisionMask & (int) CollisionGroup.GhostImpassable) != 0)
                continue;

            var foo = _xform.GetWorldPosition(ent, xformQuery) - worldPos;
            _throwing.TryThrow(ent, foo * 5, strength, uid, 0);
        }
    }
}

