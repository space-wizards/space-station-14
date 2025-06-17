using Content.Shared.Popups;
using Content.Shared.Actions;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.WebPlacer;

/// <summary>
///     System for giving the component owner (probably a spider) an action to spawn entites around itself.
/// </summary>
public sealed class WebPlacerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;

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
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
        {
            _popup.PopupClient(Loc.GetString(webPlacer.Comp.MessageOffGrid), args.Performer, args.Performer);
            return;
        }

        // Get coordinates and spawn webs if the coordinates are valid.
        var success = false;
        foreach (var vect in webPlacer.Comp.OffsetVectors)
        {
            var pos = xform.Coordinates.Offset(vect);
            if (!IsValidTile(pos, (grid.Value, gridComp), webPlacer.Comp))
                continue;

            PredictedSpawnAtPosition(webPlacer.Comp.WebPrototype, pos);
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
    private bool IsValidTile(EntityCoordinates coords, Entity<MapGridComponent> mapGrid, WebPlacerComponent comp)
    {
        // Don't place webs in space
        if (!_map.TryGetTileRef(mapGrid.Owner, mapGrid.Comp, coords, out var tileRef) ||
            tileRef.IsSpace(_tile))
            return false;

        // Check whitelist and blacklist
        foreach (var entity in _lookup.GetEntitiesIntersecting(coords, LookupFlags.Uncontained))
            if (!_whitelist.CheckBoth(entity, comp.DestinationBlacklist, comp.DestinationWhitelist))
                return false;

        return true;
    }
}
