using System.Linq;
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
/// Spawns entities (probably webs) around the component owner. Action handled by <see cref="SharedWebPlacerSystem"/>.
/// </summary>
public sealed class WebPlacerSystem : SharedWebPlacerSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WebPlacerComponent, SpiderWebActionEvent>(OnSpawnNet);
    }

    private void OnSpawnNet(EntityUid uid, WebPlacerComponent component, SpiderWebActionEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(uid);
        var grid = transform.GridUid;

        // Instantly fail in space
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
        {
            _popup.PopupEntity(Loc.GetString(component.MessageOffGrid), args.Performer, args.Performer);
            return;
        }

        var coords = transform.Coordinates;
        var spawnPos = component.OffsetVectors.Select(v => coords.Offset(v));

        bool success = false;
        foreach (var pos in spawnPos)
        {
            if (!IsValidTile(pos, component.DestinationWhitelist, component.DestinationBlacklist, grid.Value, gridComp))
                continue;

            Spawn(component.WebPrototype, pos);
            success = true;
        }

        // Return if nothing was spawned
        if (!success)
        {
            _popup.PopupEntity(Loc.GetString(component.MessageFail), args.Performer, args.Performer);
            return;
        }

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString(component.MessageSuccess), args.Performer, args.Performer);

        if (component.WebSound != null)
            _audio.PlayPvs(component.WebSound, uid);
    }

    private bool IsValidTile(EntityCoordinates coords, EntityWhitelist? whitelist, EntityWhitelist? blacklist, EntityUid grid, MapGridComponent gridComp)
    {
        // Don't place webs in space
        if (!_map.TryGetTileRef(grid, gridComp, coords, out var tileRef) ||
            tileRef.IsSpace(_tile))
            return false;

        // Don't place webs on webs
        if (blacklist != null)
            foreach (var entity in _lookup.GetEntitiesIntersecting(coords, LookupFlags.Uncontained))
                if (_whitelistSystem.IsBlacklistPass(blacklist, entity))
                    return false;

        // Only place webs on webs
        if (whitelist != null)
        {
            foreach (var entity in _lookup.GetEntitiesIntersecting(coords, LookupFlags.Uncontained))
                if (_whitelistSystem.IsWhitelistPass(whitelist, entity))
                    return true;

            return false;
        }

        return true;
    }
}
