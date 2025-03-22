using Content.Server.Popups;
using Content.Shared.Maps;
using Content.Shared.WebPlacer;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.WebPlacer;

/// <summary>
/// Spawns entities (probably webs) around the component owner when using the component's action.
/// </summary>
/// <seealso cref="WebPlacerComponent"/>
public sealed class WebPlacerSystem : SharedWebPlacerSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WebPlacerComponent, SpiderWebActionEvent>(OnSpawnWeb);
    }

    private void OnSpawnWeb(Entity<WebPlacerComponent> webPlacer, ref SpiderWebActionEvent args)
    {
        if (args.Handled)
            return;

        var xform = Transform(webPlacer.Owner);
        var grid = xform.GridUid;

        // Instantly fail in space.
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
        {
            _popup.PopupEntity(Loc.GetString(webPlacer.Comp.MessageOffGrid), args.Performer, args.Performer);
            return;
        }

        // Get coordinates and spawn webs if the coordinates are valid.
        bool success = false;
        foreach (var vect in webPlacer.Comp.OffsetVectors)
        {
            var pos = xform.Coordinates.Offset(vect);
            if (!IsValidTile(pos, webPlacer.Comp.DestinationWhitelist, webPlacer.Comp.DestinationBlacklist, (grid.Value, gridComp)))
                continue;

            Spawn(webPlacer.Comp.WebPrototype, pos);
            success = true;
        }

        // Return unhandled if nothing was spawned so that the action doesn't go on cooldown.
        if (!success)
        {
            _popup.PopupEntity(Loc.GetString(webPlacer.Comp.MessageNoSpawn), args.Performer, args.Performer);
            return;
        }

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString(webPlacer.Comp.MessageSuccess), args.Performer, args.Performer);

        if (webPlacer.Comp.WebSound != null)
            _audio.PlayPvs(webPlacer.Comp.WebSound, webPlacer.Owner);
    }

    private bool IsValidTile(EntityCoordinates coords, EntityWhitelist? whitelist, EntityWhitelist? blacklist, Entity<MapGridComponent> mapGrid)
    {
        // Don't place webs in space
        if (!_map.TryGetTileRef(mapGrid.Owner, mapGrid.Comp, coords, out var tileRef) ||
            tileRef.IsSpace(_tile))
            return false;

        // Don't place webs on webs
        if (blacklist != null)
            foreach (var entity in _lookup.GetEntitiesIntersecting(coords, LookupFlags.Uncontained))
                if (_whitelist.IsBlacklistPass(blacklist, entity))
                    return false;

        // Only place webs on webs
        if (whitelist != null)
        {
            foreach (var entity in _lookup.GetEntitiesIntersecting(coords, LookupFlags.Uncontained))
                if (_whitelist.IsWhitelistPass(whitelist, entity))
                    return true;

            return false;
        }

        return true;
    }
}
