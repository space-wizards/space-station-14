using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.RCD.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.RCD.Systems;

public sealed class RCDSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly int RCDModeCount = Enum.GetValues(typeof(RcdMode)).Length;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RCDComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<RCDComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<RCDComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RCDComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RCDComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RCDComponent, RCDDoAfterEvent>(OnDoAfter);
    }

    private void OnGetState(EntityUid uid, RCDComponent comp, ref ComponentGetState args)
    {
        args.State = new RCDComponentState(comp.MaxCharges, comp.Charges, comp.Delay, comp.Mode, comp.Floor);
    }

    private void OnHandleState(EntityUid uid, RCDComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not RCDComponentState state)
            return;

        comp.MaxCharges = state.MaxCharges;
        comp.Charges = state.Charges;
        comp.Delay = state.Delay;
        comp.Mode = state.Mode;
        comp.Floor = state.Floor;
    }

    private void OnExamine(EntityUid uid, RCDComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var msg = Loc.GetString("rcd-component-examine-detail-count",
            ("mode", comp.Mode), ("charges", comp.Charges));
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

        // fail fast if out of ammo
        if (comp.Charges <= 0)
        {
            ClientPopup(Loc.GetString("rcd-component-no-ammo-message"), uid, user);
            return;
        }

        var location = args.ClickLocation;
        // Initial validity check
        if (!location.IsValid(EntityManager))
            return;

        var doAfterArgs = new DoAfterArgs(user, comp.Delay, new RCDDoAfterEvent(comp.Mode, location), uid, target: args.Target, used: uid)
        {
            BreakOnDamage = true,
            NeedHand = true,
        };

        args.Handled = true;
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, RCDComponent comp, RCDDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !_timing.IsFirstTimePredicted)
            return;

        var user = args.User;
        var location = args.Location;
        // Try to fix it (i.e. if clicking on space)
        // Note: Ideally there'd be a better way, but there isn't right now.
        // TODO: see if there is a better way now
        var gridIdOpt = location.GetGridUid(EntityManager);
        if (!(gridIdOpt is EntityUid gridId) || !gridId.IsValid())
        {
            location = location.AlignWithClosestGridTile();
            gridIdOpt = location.GetGridUid(EntityManager);
            // Check if fixing it failed / get final grid ID
            if (!(gridIdOpt is EntityUid gridId2) || !gridId2.IsValid())
                return;
            gridId = gridId2;
        }

        var grid = _mapMan.GetGrid(gridId);
        var tile = grid.GetTileRef(location);
        var snapPos = grid.TileIndicesFor(location);

        // using mode from the doafter args since user mightve changed it on the rcd after starting it
        switch (args.Mode)
        {
            //Floor mode just needs the tile to be a space tile (subFloor)
            case RcdMode.Floors:
                if (!tile.Tile.IsEmpty)
                {
                    ClientPopup(Loc.GetString("rcd-component-cannot-build-floor-tile-not-empty-message"), uid, user);
                    return;
                }

                grid.SetTile(snapPos, new Tile(_tileDefMan[comp.Floor].TileId));
                _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to set grid: {tile.GridUid} {snapPos} to {comp.Floor}");
                break;
            //We don't want to place a space tile on something that's already a space tile. Let's do the inverse of the last check.
            case RcdMode.Deconstruct:
                if (!tile.IsBlockedTurf(true)) // Delete the turf
                {
                    grid.SetTile(snapPos, Tile.Empty);
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to set grid: {tile.GridUid} tile: {snapPos} to space");
                }
                else if (args.Target is {Valid: true} target) // Delete the targeted thing
                {
                    if (!_tag.HasTag(target, "RCDDeconstructWhitelist"))
                    {
                        ClientPopup(Loc.GetString("rcd-component-deconstruct-target-not-on-whitelist-message"), uid, user);
                        return;
                    }

                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to delete {ToPrettyString(target):target}");
                    QueueDel(target);
                }
                else
                {
                    ClientPopup(Loc.GetString("rcd-component-tile-obstructed-message"), uid, user);
                    return;
                }
                break;
            //Walls are a special behaviour, and require us to build a new object with a transform rather than setting a grid tile,
            // thus we early return to avoid the tile set code.
            case RcdMode.Walls:
                if (tile.Tile.IsEmpty)
                {
                    ClientPopup(Loc.GetString("rcd-component-cannot-build-wall-tile-not-empty-message"), uid, user);
                    return;
                }

                if (tile.IsBlockedTurf(true))
                {
                    ClientPopup(Loc.GetString("rcd-component-tile-obstructed-message"), uid, user);
                    return;
                }

                // only spawn on the server
                if (_net.IsServer)
                {
                    var ent = Spawn("WallSolid", grid.GridTileToLocal(snapPos));
                    Transform(ent).LocalRotation = Angle.Zero; // Walls always need to point south.
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to spawn {ToPrettyString(ent)} at {snapPos} on grid {tile.GridUid}");
                }
                break;
            case RcdMode.Airlock:
                if (tile.Tile.IsEmpty)
                {
                    ClientPopup(Loc.GetString("rcd-component-cannot-build-airlock-tile-not-empty-message"), uid, user);
                    return;
                }
                if (tile.IsBlockedTurf(true))
                {
                    ClientPopup(Loc.GetString("rcd-component-tile-obstructed-message"), uid, user);
                    return;
                }

                // only spawn on the server
                if (_net.IsServer)
                {
                    var airlock = Spawn("Airlock", grid.GridTileToLocal(snapPos));
                    Transform(airlock).LocalRotation = Transform(uid).LocalRotation; //Now apply icon smoothing.
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to spawn {ToPrettyString(airlock)} at {snapPos} on grid {tile.GridUid}");
                }
                break;
            default:
                args.Handled = true;
                return; //I don't know why this would happen, but sure I guess. Get out of here invalid state!
        }

        _audio.PlayPredicted(comp.SuccessSound, uid, user);
        comp.Charges--;
        args.Handled = true;
    }

    private void NextMode(EntityUid uid, RCDComponent comp, EntityUid user)
    {
        _audio.PlayPredicted(comp.SwapModeSound, uid, user);

        var mode = (int) comp.Mode;
        mode = ++mode % RCDModeCount;
        comp.Mode = (RcdMode) mode;

        var msg = Loc.GetString("rcd-component-change-mode", ("mode", comp.Mode.ToString()));
        ClientPopup(msg, uid, user);
    }

    // TODO: see if this isnt implemented in some utility it seems fairly common
    private void ClientPopup(string msg, EntityUid uid, EntityUid user)
    {
        if (_net.IsClient && _timing.IsFirstTimePredicted)
            _popup.PopupEntity(msg, uid, user);
    }
}

[Serializable, NetSerializable]
public sealed class RCDDoAfterEvent : DoAfterEvent
{
    [DataField("mode", required: true)]
    public readonly RcdMode Mode = default!;

    [DataField("location", required: true)]
    public readonly EntityCoordinates Location = default!;

    private RCDDoAfterEvent()
    {
    }

    public RCDDoAfterEvent(RcdMode mode, EntityCoordinates location)
    {
        Mode = mode;
        Location = location;
    }

    public override DoAfterEvent Clone() => this;
}
