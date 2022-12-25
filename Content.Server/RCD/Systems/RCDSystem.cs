using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.RCD.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server.RCD.Systems
{
    public sealed class RCDSystem : EntitySystem
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

        private readonly int RCDModeCount = Enum.GetValues(typeof(RcdMode)).Length;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RCDComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<RCDComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<RCDComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnExamine(EntityUid uid, RCDComponent component, ExaminedEvent args)
        {
            var msg = Loc.GetString("rcd-component-examine-detail-count",
                ("mode", component.Mode), ("ammoCount", component.CurrentAmmo));
            args.PushMarkup(msg);
        }

        private void OnUseInHand(EntityUid uid, RCDComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            NextMode(uid, component, args.User);
            args.Handled = true;
        }

        private async void OnAfterInteract(EntityUid uid, RCDComponent rcd, AfterInteractEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (rcd.CancelToken != null)
            {
                rcd.CancelToken?.Cancel();
                rcd.CancelToken = null;
                args.Handled = true;
                return;
            }

            if (!args.ClickLocation.IsValid(EntityManager)) return;

            var clickLocationMod = args.ClickLocation;
            // Initial validity check
            if (!clickLocationMod.IsValid(EntityManager))
                return;
            // Try to fix it (i.e. if clicking on space)
            // Note: Ideally there'd be a better way, but there isn't right now.
            var gridIdOpt = clickLocationMod.GetGridUid(EntityManager);
            if (!(gridIdOpt is EntityUid gridId) || !gridId.IsValid())
            {
                clickLocationMod = clickLocationMod.AlignWithClosestGridTile();
                gridIdOpt = clickLocationMod.GetGridUid(EntityManager);
                // Check if fixing it failed / get final grid ID
                if (!(gridIdOpt is EntityUid gridId2) || !gridId2.IsValid())
                    return;
                gridId = gridId2;
            }

            var mapGrid = _mapManager.GetGrid(gridId);
            var tile = mapGrid.GetTileRef(clickLocationMod);
            var snapPos = mapGrid.TileIndicesFor(clickLocationMod);

            //No changing mode mid-RCD
            var startingMode = rcd.Mode;
            args.Handled = true;
            var user = args.User;

            //Using an RCD isn't instantaneous
            rcd.CancelToken = new CancellationTokenSource();
            var doAfterEventArgs = new DoAfterEventArgs(user, rcd.Delay, rcd.CancelToken.Token, args.Target)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true,
                ExtraCheck = () => IsRCDStillValid(rcd, args, mapGrid, tile, startingMode) //All of the sanity checks are here
            };

            var result = await _doAfterSystem.WaitDoAfter(doAfterEventArgs);

            rcd.CancelToken = null;

            if (result == DoAfterStatus.Cancelled)
                return;

            switch (rcd.Mode)
            {
                //Floor mode just needs the tile to be a space tile (subFloor)
                case RcdMode.Floors:
                    mapGrid.SetTile(snapPos, new Tile(_tileDefinitionManager["FloorSteel"].TileId));
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to set grid: {tile.GridUid} {snapPos} to FloorSteel");
                    break;
                //We don't want to place a space tile on something that's already a space tile. Let's do the inverse of the last check.
                case RcdMode.Deconstruct:
                    if (!tile.IsBlockedTurf(true)) //Delete the turf
                    {
                        mapGrid.SetTile(snapPos, Tile.Empty);
                        _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to set grid: {tile.GridUid} tile: {snapPos} to space");
                    }
                    else //Delete what the user targeted
                    {
                        if (args.Target is {Valid: true} target)
                        {
                            _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to delete {ToPrettyString(target):target}");
                            QueueDel(target);
                        }
                    }
                    break;
                //Walls are a special behaviour, and require us to build a new object with a transform rather than setting a grid tile,
                // thus we early return to avoid the tile set code.
                case RcdMode.Walls:
                    var ent = EntityManager.SpawnEntity("WallSolid", mapGrid.GridTileToLocal(snapPos));
                    Transform(ent).LocalRotation = Angle.Zero; // Walls always need to point south.
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to spawn {ToPrettyString(ent)} at {snapPos} on grid {mapGrid.Owner}");
                    break;
                case RcdMode.Airlock:
                    var airlock = EntityManager.SpawnEntity("Airlock", mapGrid.GridTileToLocal(snapPos));
                    Transform(airlock).LocalRotation = Transform(rcd.Owner).LocalRotation; //Now apply icon smoothing.
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(args.User):user} used RCD to spawn {ToPrettyString(airlock)} at {snapPos} on grid {mapGrid.Owner}");
                    break;
                default:
                    args.Handled = true;
                    return; //I don't know why this would happen, but sure I guess. Get out of here invalid state!
            }

            SoundSystem.Play(rcd.SuccessSound.GetSound(), Filter.Pvs(uid, entityManager: EntityManager), rcd.Owner);
            rcd.CurrentAmmo--;
            args.Handled = true;
        }

        private bool IsRCDStillValid(RCDComponent rcd, AfterInteractEvent eventArgs, MapGridComponent mapGrid, TileRef tile, RcdMode startingMode)
        {
            //Less expensive checks first. Failing those ones, we need to check that the tile isn't obstructed.
            if (rcd.CurrentAmmo <= 0)
            {
                _popup.PopupEntity(Loc.GetString("rcd-component-no-ammo-message"), rcd.Owner, eventArgs.User);
                return false;
            }

            if (rcd.Mode != startingMode)
            {
                return false;
            }

            var unobstructed = eventArgs.Target == null
                ? _interactionSystem.InRangeUnobstructed(eventArgs.User, mapGrid.GridTileToWorld(tile.GridIndices), popup: true)
                : _interactionSystem.InRangeUnobstructed(eventArgs.User, eventArgs.Target.Value, popup: true);

            if (!unobstructed)
                return false;

            switch (rcd.Mode)
            {
                //Floor mode just needs the tile to be a space tile (subFloor)
                case RcdMode.Floors:
                    if (!tile.Tile.IsEmpty)
                    {
                        _popup.PopupEntity(Loc.GetString("rcd-component-cannot-build-floor-tile-not-empty-message"), rcd.Owner, eventArgs.User);
                        return false;
                    }

                    return true;
                //We don't want to place a space tile on something that's already a space tile. Let's do the inverse of the last check.
                case RcdMode.Deconstruct:
                    if (tile.Tile.IsEmpty)
                    {
                        return false;
                    }

                    //They tried to decon a turf but the turf is blocked
                    if (eventArgs.Target == null && tile.IsBlockedTurf(true))
                    {
                        _popup.PopupEntity(Loc.GetString("rcd-component-tile-obstructed-message"), rcd.Owner, eventArgs.User);
                        return false;
                    }
                    //They tried to decon a non-turf but it's not in the whitelist
                    if (eventArgs.Target != null && !_tagSystem.HasTag(eventArgs.Target.Value, "RCDDeconstructWhitelist"))
                    {
                        _popup.PopupEntity(Loc.GetString("rcd-component-deconstruct-target-not-on-whitelist-message"), rcd.Owner, eventArgs.User);
                        return false;
                    }

                    return true;
                //Walls are a special behaviour, and require us to build a new object with a transform rather than setting a grid tile, thus we early return to avoid the tile set code.
                case RcdMode.Walls:
                    if (tile.Tile.IsEmpty)
                    {
                        _popup.PopupEntity(Loc.GetString("rcd-component-cannot-build-wall-tile-not-empty-message"), rcd.Owner, eventArgs.User);
                        return false;
                    }

                    if (tile.IsBlockedTurf(true))
                    {
                        _popup.PopupEntity(Loc.GetString("rcd-component-tile-obstructed-message"), rcd.Owner, eventArgs.User);
                        return false;
                    }
                    return true;
                case RcdMode.Airlock:
                    if (tile.Tile.IsEmpty)
                    {
                        _popup.PopupEntity(Loc.GetString("rcd-component-cannot-build-airlock-tile-not-empty-message"), rcd.Owner, eventArgs.User);
                        return false;
                    }
                    if (tile.IsBlockedTurf(true))
                    {
                        _popup.PopupEntity(Loc.GetString("rcd-component-tile-obstructed-message"), rcd.Owner, eventArgs.User);
                        return false;
                    }
                    return true;
                default:
                    return false; //I don't know why this would happen, but sure I guess. Get out of here invalid state!
            }
        }

        private void NextMode(EntityUid uid, RCDComponent rcd, EntityUid user)
        {
            SoundSystem.Play(rcd.SwapModeSound.GetSound(), Filter.Pvs(uid, entityManager: EntityManager), uid);

            var mode = (int) rcd.Mode;
            mode = ++mode % RCDModeCount;
            rcd.Mode = (RcdMode) mode;

            var msg = Loc.GetString("rcd-component-change-mode", ("mode", rcd.Mode.ToString()));
            _popup.PopupEntity(msg, rcd.Owner, user);
        }
    }
}
