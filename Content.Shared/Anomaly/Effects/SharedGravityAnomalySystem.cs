using System.Linq;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Throwing;
using Robust.Shared.Map;

namespace Content.Shared.Anomaly.Effects;

public abstract class SharedGravityAnomalySystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GravityAnomalyComponent, AnomalyPulseEvent>(OnAnomalyPulse);
        SubscribeLocalEvent<GravityAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnAnomalyPulse(EntityUid uid, GravityAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var xform = Transform(uid);
        var range = component.MaxThrowRange * args.Severity;
        var strength = component.MaxThrowStrength * args.Severity;
        var lookup = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic | LookupFlags.Sundries);
        foreach (var ent in lookup)
        {
            var tempXform = Transform(ent);
            var foo = tempXform.MapPosition.Position - xform.MapPosition.Position;
            _throwing.TryThrow(ent, foo * 10, strength, uid, 0);
        }
    }

    private void OnSupercritical(EntityUid uid, GravityAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        var xform = Transform(uid);
        if (!_map.TryGetGrid(xform.GridUid, out var grid))
            return;

        var worldPos = _xform.GetWorldPosition(xform);
        var tileref = grid.GetTilesIntersecting(new Circle(worldPos, component.SpaceRange)).ToArray();
        var tiles = tileref.Select(t => (t.GridIndices, Tile.Empty)).ToList();
        grid.SetTiles(tiles);

        var range = component.MaxThrowRange * 2;
        var strength = component.MaxThrowStrength * 2;
        var lookup = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic | LookupFlags.Sundries);
        foreach (var ent in lookup)
        {
            var tempXform = Transform(ent);

            var foo = tempXform.MapPosition.Position - xform.MapPosition.Position;
            _throwing.TryThrow(ent, foo * 5, strength, uid, 0);
        }
    }
}

