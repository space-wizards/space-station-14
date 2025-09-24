using Content.Shared.Actions;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.WebPlacer;

/// <summary>
/// System for giving the component owner (probably a spider) an action to spawn entities around itself.
/// </summary>
public abstract class SharedWebPlacerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    /// <summary>
    /// A recycled hashset used to check turfs for spiderwebs.
    /// </summary>
    private readonly HashSet<EntityUid> _webs = [];

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WebPlacerComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<WebPlacerComponent, SpiderWebActionEvent>(OnSpawnWeb);
    }

    /// <summary>
    /// Give the entity its spawning action.
    /// </summary>
    private void OnInit(Entity<WebPlacerComponent> ent, ref MapInitEvent args)
    {
        _action.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.SpawnWebAction, ent.Owner);
    }

    /// <summary>
    /// Spawn webs when using an action.
    /// </summary>
    private void OnSpawnWeb(Entity<WebPlacerComponent> ent, ref SpiderWebActionEvent args)
    {
        if (args.Handled)
            return;

        var xform = Transform(args.Performer);

        // Instantly fail in space.
        if (!HasComp<MapGridComponent>(xform.GridUid))
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.MessageOffGrid), args.Performer, args.Performer);
            return;
        }

        var result = TrySpawnWebs(ent, xform.Coordinates);

        // Return unhandled if nothing was spawned so that the action doesn't go on cooldown.
        if (!result)
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.MessageNoSpawn), args.Performer, args.Performer);
            return;
        }

        args.Handled = true;
        _popup.PopupClient(Loc.GetString(ent.Comp.MessageSuccess), args.Performer, args.Performer);
    }

    /// <summary>
    /// Spawns webs around a location.
    /// </summary>
    /// <param name="ent">The spider.</param>
    /// <param name="coords">Center location webs spawn around.</param>
    /// <returns>True if any webs spawned.</returns>
    protected bool TrySpawnWebs(Entity<WebPlacerComponent> ent, EntityCoordinates coords)
    {
        var (uid, comp) = ent;

        // Get coordinates and spawn webs if the coordinates are valid.
        var success = false;
        foreach (var vector in comp.OffsetVectors)
        {
            var pos = coords.Offset(vector);
            if (!IsValidTile(comp, pos))
                continue;

            PredictedSpawnAtPosition(comp.SpawnId, pos);
            success = true;
        }

        if (success && comp.WebSound != null)
            _audio.PlayPredicted(comp.WebSound, uid, uid);

        return success;
    }

    /// <returns>False if coords are in space. False if whitelisting fails. True otherwise.</returns>
    private bool IsValidTile(WebPlacerComponent comp, EntityCoordinates coords)
    {
        // Invalid in space or non-existent tile
        if (!_turf.TryGetTileRef(coords, out var tileRef) || _turf.IsSpace(tileRef.Value))
            return false;

        _webs.Clear();
        _lookup.GetEntitiesInTile(tileRef.Value, _webs, LookupFlags.Uncontained);

        // Invalid if anything fails the whitelist
        foreach (var ent in _webs)
        {
            if (!_whitelist.CheckBoth(ent, comp.DestinationBlacklist, comp.DestinationWhitelist))
                return false;
        }

        // Valid otherwise
        return true;
    }
}
