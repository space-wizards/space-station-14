using Content.Shared.Popups;
using Content.Shared.Actions;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.WebPlacer;

/// <summary>
///     System for giving the component owner (probably a spider) an action to spawn entities around itself.
/// </summary>
public sealed class WebPlacerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    /// <summary>
    ///     A recycled hashset used to check turfs for spiderwebs.
    /// </summary>
    private readonly HashSet<EntityUid> _webs = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WebPlacerComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<WebPlacerComponent, SpiderWebActionEvent>(OnSpawnWeb);
    }

    private void OnInit(Entity<WebPlacerComponent> webPlacer, ref MapInitEvent args)
    {
        _action.AddAction(webPlacer.Owner, ref webPlacer.Comp.ActionEntity, webPlacer.Comp.SpawnWebAction, webPlacer.Owner);
    }

    private void OnSpawnWeb(Entity<WebPlacerComponent> webPlacer, ref SpiderWebActionEvent args)
    {
        if (args.Handled)
            return;

        var xform = Transform(args.Performer);
        var grid = xform.GridUid;

        // Instantly fail in space.
        if (!HasComp<MapGridComponent>(grid))
        {
            _popup.PopupClient(Loc.GetString(webPlacer.Comp.MessageOffGrid), args.Performer, args.Performer);
            return;
        }

        // Get coordinates and spawn webs if the coordinates are valid.
        var success = false;
        foreach (var vector in webPlacer.Comp.OffsetVectors)
        {
            var pos = xform.Coordinates.Offset(vector);
            if (!IsValidTile(pos, webPlacer.Comp))
                continue;

            PredictedSpawnAtPosition(webPlacer.Comp.SpawnId, pos);
            success = true;
        }

        // Return unhandled if nothing was spawned so that the action doesn't go on cooldown.
        if (!success)
        {
            _popup.PopupClient(Loc.GetString(webPlacer.Comp.MessageNoSpawn), args.Performer, args.Performer);
            return;
        }

        args.Handled = true;
        _popup.PopupClient(Loc.GetString(webPlacer.Comp.MessageSuccess), args.Performer, args.Performer);

        if (webPlacer.Comp.WebSound != null)
            _audio.PlayPredicted(webPlacer.Comp.WebSound, webPlacer.Owner, webPlacer.Owner);
    }

    /// <returns>False if coords are in space. False if whitelisting fails. True otherwise.</returns>
    private bool IsValidTile(EntityCoordinates coords, WebPlacerComponent comp)
    {
        // Invalid in space or non-existent tile
        if (!_turf.TryGetTileRef(coords, out var tileRef) || _turf.IsSpace(tileRef.Value))
            return false;

        _webs.Clear();
        _lookup.GetEntitiesInTile(tileRef.Value, _webs, LookupFlags.Uncontained);

        // Invalid if failing whitelist
        foreach (var ent in _webs)
        {
            if (!_whitelist.CheckBoth(ent, comp.DestinationBlacklist, comp.DestinationWhitelist))
                return false;
        }

        // Valid otherwise
        return true;
    }
}
