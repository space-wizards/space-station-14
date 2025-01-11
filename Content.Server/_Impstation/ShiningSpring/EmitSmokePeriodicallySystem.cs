using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._Impstation.ShiningSpring;

/// <summary>
/// This handles entities that should emit smoke periodically.
/// not a trigger because that would require a full rewrite of triggerSystem for my specific usecase
/// </summary>
public sealed class EmitSmokePeriodicallySystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SpreaderSystem _spreader = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<EmitSmokePeriodicallyComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.timer += frameTime;
            if (!(comp.timer > comp.EmissionPeriod))
                continue;

            comp.timer = 0;

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
            var ent = Spawn(comp.SmokePrototype, coords.SnapToGrid());
            if (!TryComp<SmokeComponent>(ent, out var smoke))
            {
                Log.Error($"Smoke prototype {comp.SmokePrototype} was missing SmokeComponent");
                Del(ent);
                return;
            }

            _smoke.StartSmoke(ent, comp.Solution.Clone(), comp.Duration, comp.SpreadAmount, smoke);
        }
    }
}
