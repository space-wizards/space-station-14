using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Server.Explosion.EntitySystems;

/// <summary>
/// Handles creating smoke when <see cref="SmokeOnTriggerComponent"/> is triggered.
/// </summary>
public sealed class SmokeOnTriggerSystem : SharedSmokeOnTriggerSystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmokeOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(EntityUid uid, SmokeOnTriggerComponent comp, TriggerEvent args)
    {
        var xform = Transform(uid);
        if (!_mapMan.TryFindGridAt(xform.MapPosition, out _, out var grid) ||
            !grid.TryGetTileRef(xform.Coordinates, out var tileRef) ||
            tileRef.Tile.IsSpace())
        {
            return;
        }

        var coords = grid.MapToGrid(xform.MapPosition);
        var ent = Spawn(comp.SmokePrototype, coords.SnapToGrid());
        if (!TryComp<SmokeComponent>(ent, out var smoke))
        {
            Logger.Error($"Smoke prototype {comp.SmokePrototype} was missing SmokeComponent");
            Del(ent);
            return;
        }

        _smoke.StartSmoke(ent, comp.Solution, comp.Duration, comp.SpreadAmount, smoke);
    }
}
