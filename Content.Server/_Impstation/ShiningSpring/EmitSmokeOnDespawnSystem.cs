using Content.Server._Impstation.ShiningSpring;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Spawners;

namespace Content.Server._Impstation.ShiningSpring;

/// <summary>
/// This handles entities that should emit smoke when despawned through a TimedDespawnComponent
/// not a trigger because that would require a full rewrite of triggerSystem for my specific usecase
/// </summary>
public sealed class EmitSmokeOnDespawnSystem : EntitySystem
{

    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SpreaderSystem _spreader = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EmitSmokeOnDespawnComponent, TimedDespawnEvent>(OnTimedDespawn);
    }

    private void OnTimedDespawn(Entity<EmitSmokeOnDespawnComponent> ent, ref TimedDespawnEvent args)
    {

        var uid = ent.Owner;
        var comp = ent.Comp;

        var xform = Transform(uid);
        var mapCoords = _transform.GetMapCoordinates(uid, xform);
        if (!_mapMan.TryFindGridAt(mapCoords, out var gridUid, out var gridComp) ||
            !_mapSystem.TryGetTileRef(gridUid, gridComp, xform.Coordinates, out var tileRef) ||
            tileRef.Tile.IsEmpty)
        {
            return;
        }

        if (_spreader.RequiresFloorToSpread(comp.SmokePrototype.ToString()) && tileRef.Tile.IsSpace())
            return;

        var coords = _mapSystem.MapToGrid(gridUid, mapCoords);
        var smokeEnt = Spawn(comp.SmokePrototype, coords.SnapToGrid());
        if (!TryComp<SmokeComponent>(smokeEnt, out var smoke))
        {
            Log.Error($"Smoke prototype {comp.SmokePrototype} was missing SmokeComponent");
            Del(smokeEnt);
            return;
        }

        _smoke.StartSmoke(smokeEnt, comp.Solution.Clone(), comp.Duration, comp.SpreadAmount, smoke);
    }
}
