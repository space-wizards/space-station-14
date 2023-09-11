using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.RCD.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.RCD.Systems;

public sealed class RCDSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly int RcdModeCount = Enum.GetValues(typeof(RcdMode)).Length;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RCDComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RCDComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RCDComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RCDComponent, RCDDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RCDComponent, DoAfterAttemptEvent<RCDDoAfterEvent>>(OnDoAfterAttempt);
    }

    private void OnExamine(EntityUid uid, RCDComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var msg = Loc.GetString("rcd-component-examine-detail", ("mode", comp.Mode));
        args.PushMarkup(msg);
    }

    private void OnUseInHand(EntityUid uid, RCDComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        NextMode(uid, comp, args.User);
        args.Handled = true;
    }

    private void OnAfterInteract(EntityUid uid, RCDComponent comp, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        var user = args.User;

        TryComp<LimitedChargesComponent>(uid, out var charges);
        if (_charges.IsEmpty(uid, charges))
        {
            _popup.PopupClient(Loc.GetString("rcd-component-no-ammo-message"), uid, user);
            return;
        }

        var location = args.ClickLocation;
        // Initial validity check
        if (!location.IsValid(EntityManager))
            return;

        var gridId = location.GetGridUid(EntityManager);
        if (!HasComp<MapGridComponent>(gridId))
        {
            location = location.AlignWithClosestGridTile();
            gridId = location.GetGridUid(EntityManager);
            // Check if fixing it failed / get final grid ID
            if (!HasComp<MapGridComponent>(gridId))
                return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, user, comp.Delay, new RCDDoAfterEvent(GetNetCoordinates(location), comp.Mode), uid, target: args.Target, used: uid)
        {
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnUserMove = true,
            BreakOnTargetMove = args.Target != null,
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        args.Handled = true;
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfterAttempt(EntityUid uid, RCDComponent comp, DoAfterAttemptEvent<RCDDoAfterEvent> args)
    {
        // sus client crash why
        if (args.Event?.DoAfter?.Args == null)
            return;

        var location = GetCoordinates(args.Event.Location);

        var gridId = location.GetGridUid(EntityManager);
        if (!HasComp<MapGridComponent>(gridId))
        {
            location = location.AlignWithClosestGridTile();
            gridId = location.GetGridUid(EntityManager);
            // Check if fixing it failed / get final grid ID
            if (!HasComp<MapGridComponent>(gridId))
                return;
        }

        var mapGrid = _mapMan.GetGrid(gridId.Value);
        var tile = mapGrid.GetTileRef(location);

        if (!IsRCDStillValid(uid, comp, args.Event.User, args.Event.Target, mapGrid, tile, args.Event.StartingMode))
            args.Cancel();
    }

    private void OnDoAfter(EntityUid uid, RCDComponent comp, RCDDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !_timing.IsFirstTimePredicted)
            return;

        var user = args.User;
        var location = GetCoordinates(args.Location);

        var gridId = location.GetGridUid(EntityManager);
        if (!HasComp<MapGridComponent>(gridId))
        {
            location = location.AlignWithClosestGridTile();
            gridId = location.GetGridUid(EntityManager);
            // Check if fixing it failed / get final grid ID
            if (!HasComp<MapGridComponent>(gridId))
                return;
        }

        var mapGrid = _mapMan.GetGrid(gridId.Value);
        var tile = mapGrid.GetTileRef(location);
        var snapPos = mapGrid.TileIndicesFor(location);

        switch (comp.Mode)
        {
            //Floor mode just needs the tile to be a space tile (subFloor)
            case RcdMode.Floors:

                mapGrid.SetTile(snapPos, new Tile(_tileDefMan[comp.Floor].TileId));
                _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to set grid: {tile.GridUid} {snapPos} to {comp.Floor}");
                break;
            //We don't want to place a space tile on something that's already a space tile. Let's do the inverse of the last check.
            case RcdMode.Deconstruct:
                if (!IsTileBlocked(tile)) // Delete the turf
                {
                    mapGrid.SetTile(snapPos, Tile.Empty);
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to set grid: {tile.GridUid} tile: {snapPos} to space");
                }
                else // Delete the targeted thing
                {
                    var target = args.Target!.Value;
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to delete {ToPrettyString(target):target}");
                    QueueDel(target);
                }
                break;
            //Walls are a special behaviour, and require us to build a new object with a transform rather than setting a grid tile,
            // thus we early return to avoid the tile set code.
            case RcdMode.Walls:
                // only spawn on the server
                if (_net.IsServer)
                {
                    var ent = Spawn("WallSolid", mapGrid.GridTileToLocal(snapPos));
                    Transform(ent).LocalRotation = Angle.Zero; // Walls always need to point south.
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to spawn {ToPrettyString(ent)} at {snapPos} on grid {tile.GridUid}");
                }
                break;
            case RcdMode.Airlock:
                // only spawn on the server
                if (_net.IsServer)
                {
                    var airlock = Spawn("Airlock", mapGrid.GridTileToLocal(snapPos));
                    Transform(airlock).LocalRotation = Transform(uid).LocalRotation; //Now apply icon smoothing.
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to spawn {ToPrettyString(airlock)} at {snapPos} on grid {tile.GridUid}");
                }
                break;
            default:
                args.Handled = true;
                return; //I don't know why this would happen, but sure I guess. Get out of here invalid state!
        }

        _audio.PlayPredicted(comp.SuccessSound, uid, user);
        _charges.UseCharge(uid);
        args.Handled = true;
    }

    private bool IsRCDStillValid(EntityUid uid, RCDComponent comp, EntityUid user, EntityUid? target, MapGridComponent mapGrid, TileRef tile, RcdMode startingMode)
    {
        //Less expensive checks first. Failing those ones, we need to check that the tile isn't obstructed.
        if (comp.Mode != startingMode)
            return false;

        var unobstructed = target == null
            ? _interaction.InRangeUnobstructed(user, mapGrid.GridTileToWorld(tile.GridIndices), popup: true)
            : _interaction.InRangeUnobstructed(user, target.Value, popup: true);

        if (!unobstructed)
            return false;

        switch (comp.Mode)
        {
            //Floor mode just needs the tile to be a space tile (subFloor)
            case RcdMode.Floors:
                if (!tile.Tile.IsEmpty)
                {
                    _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-floor-tile-not-empty-message"), uid, user);
                    return false;
                }

                return true;
            //We don't want to place a space tile on something that's already a space tile. Let's do the inverse of the last check.
            case RcdMode.Deconstruct:
                if (tile.Tile.IsEmpty)
                    return false;

                //They tried to decon a turf but...
                if (target == null)
                {
                    // the turf is blocked
                    if (IsTileBlocked(tile))
                    {
                        _popup.PopupClient(Loc.GetString("rcd-component-tile-obstructed-message"), uid, user);
                        return false;
                    }
                    // the turf can't be destroyed (planet probably)
                    var tileDef = (ContentTileDefinition) _tileDefMan[tile.Tile.TypeId];
                    if (tileDef.Indestructible)
                    {
                        _popup.PopupClient(Loc.GetString("rcd-component-tile-indestructible-message"), uid, user);
                        return false;
                    }
                }
                //They tried to decon a non-turf but it's not in the whitelist
                else if (!_tag.HasTag(target.Value, "RCDDeconstructWhitelist"))
                {
                    _popup.PopupClient(Loc.GetString("rcd-component-deconstruct-target-not-on-whitelist-message"), uid, user);
                    return false;
                }

                return true;
            //Walls are a special behaviour, and require us to build a new object with a transform rather than setting a grid tile, thus we early return to avoid the tile set code.
            case RcdMode.Walls:
                if (tile.Tile.IsEmpty)
                {
                    _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-wall-tile-not-empty-message"), uid, user);
                    return false;
                }

                if (IsTileBlocked(tile))
                {
                    _popup.PopupClient(Loc.GetString("rcd-component-tile-obstructed-message"), uid, user);
                    return false;
                }
                return true;
            case RcdMode.Airlock:
                if (tile.Tile.IsEmpty)
                {
                    _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-airlock-tile-not-empty-message"), uid, user);
                    return false;
                }
                if (IsTileBlocked(tile))
                {
                    _popup.PopupClient(Loc.GetString("rcd-component-tile-obstructed-message"), uid, user);
                    return false;
                }
                return true;
            default:
                return false; //I don't know why this would happen, but sure I guess. Get out of here invalid state!
        }
    }

    private void NextMode(EntityUid uid, RCDComponent comp, EntityUid user)
    {
        _audio.PlayPredicted(comp.SwapModeSound, uid, user);

        var mode = (int) comp.Mode;
        mode = ++mode % RcdModeCount;
        comp.Mode = (RcdMode) mode;
        Dirty(comp);

        var msg = Loc.GetString("rcd-component-change-mode", ("mode", comp.Mode.ToString()));
        _popup.PopupClient(msg, uid, user);
    }

    private bool IsTileBlocked(TileRef tile)
    {
        return _turf.IsTileBlocked(tile, CollisionGroup.MobMask);
    }
}

[Serializable, NetSerializable]
public sealed partial class RCDDoAfterEvent : DoAfterEvent
{
    [DataField("location", required: true)]
    public NetCoordinates Location = default!;

    [DataField("startingMode", required: true)]
    public RcdMode StartingMode = default!;

    private RCDDoAfterEvent()
    {
    }

    public RCDDoAfterEvent(NetCoordinates location, RcdMode startingMode)
    {
        Location = location;
        StartingMode = startingMode;
    }

    public override DoAfterEvent Clone() => this;
}
