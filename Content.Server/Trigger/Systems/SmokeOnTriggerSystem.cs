using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Trigger.Systems;

/// <summary>
/// Handles creating smoke when <see cref="SmokeOnTriggerComponent"/> is triggered.
/// </summary>
public sealed class SmokeOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SpreaderSystem _spreader = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmokeOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<SmokeOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        // TODO: move all of this into an API function in SmokeSystem
        var xform = Transform(target.Value);
        var mapCoords = _transform.GetMapCoordinates(target.Value, xform);
        if (!_mapMan.TryFindGridAt(mapCoords, out var gridUid, out var gridComp) ||
            !_map.TryGetTileRef(gridUid, gridComp, xform.Coordinates, out var tileRef) ||
            tileRef.Tile.IsEmpty)
        {
            return;
        }

        if (_spreader.RequiresFloorToSpread(ent.Comp.SmokePrototype.ToString()) && _turf.IsSpace(tileRef))
            return;

        var coords = _map.MapToGrid(gridUid, mapCoords);
        var smoke = Spawn(ent.Comp.SmokePrototype, coords.SnapToGrid());
        if (!TryComp<SmokeComponent>(smoke, out var smokeComp))
        {
            Log.Error($"Smoke prototype {ent.Comp.SmokePrototype} was missing SmokeComponent");
            Del(smoke);
            return;
        }

        _smoke.StartSmoke(smoke, ent.Comp.Solution, (float)ent.Comp.Duration.TotalSeconds, ent.Comp.SpreadAmount, smokeComp);

        args.Handled = true;
    }
}
