using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Station.Components;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Station.Systems;

/// <summary>
/// Handles respawning objects with StationRespawnComponent that get too far from the station, or get deleted.
/// </summary>
public sealed class StationRespawnSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public int MaximumRespawnAttempts { get; private set; } = 64;

    public override void Initialize()
    {
        SubscribeLocalEvent<StationRespawnComponent, MapInitEvent>(OnRespawnableSetup);
        SubscribeLocalEvent<StationRespawnComponent, ComponentShutdown>(OnRespawnableDeletion);
        SubscribeLocalEvent<StationRespawnControllerComponent, StationGridAddedEvent>(OnStationGridAdded);
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized);
    }

    public override void Update(float frameTime)
    {
        foreach (var (respawn, xform) in EntityQuery<StationRespawnComponent, TransformComponent>())
        {
            if (respawn.AttachedStation is null || Deleted(respawn.AttachedStation))
                continue;

            var tooFar = true;
            foreach (var grid in Comp<StationDataComponent>(respawn.AttachedStation.Value).Grids)
            {
                if (Deleted(grid))
                    continue; //
                var distance = (Transform(grid).WorldPosition - xform.WorldPosition).Length;
                tooFar &= distance > respawn.MaximumStationDistance;
            }

            if (!tooFar)
                continue;

            if (respawn.RespawnPopupMessage is not null)
            {
                if (!HasComp<IMapGridComponent>(xform.ParentUid) && !HasComp<IMapComponent>(xform.ParentUid))
                    _popupSystem.PopupEntity(Loc.GetString(respawn.RespawnPopupMessage, ("object", xform.Owner)), xform.ParentUid, Filter.Pvs(xform));
                else
                    _popupSystem.PopupCoordinates(Loc.GetString(respawn.RespawnPopupMessage, ("object", xform.Owner)), xform.Coordinates, Filter.Pvs(xform));
            }

            QueueDel(respawn.Owner); //Whoops, too far. Bye bye.
        }
    }

    private void OnStationInitialized(StationInitializedEvent ev)
    {
        AddComp<StationRespawnControllerComponent>(ev.Station);
    }

    private void OnStationGridAdded(EntityUid uid, StationRespawnControllerComponent component, StationGridAddedEvent args)
    {
        foreach (var respawnable in EntityQuery<StationRespawnComponent>())
        {
            if (respawnable.AttachedStation is not null)
                continue;

            var ent = _stationSystem.GetOwningStation(respawnable.Owner);
            if (ent is not null)
                respawnable.AttachedStation = ent.Value;
        }
    }

    private void OnRespawnableSetup(EntityUid uid, StationRespawnComponent component, MapInitEvent args)
    {
        var ent = _stationSystem.GetOwningStation(uid);
        if (ent is not null)
            component.AttachedStation = ent.Value;
    }

    private void OnRespawnableDeletion(EntityUid uid, StationRespawnComponent component, ComponentShutdown args)
    {
        if (component.AttachedStation is null) // We're not part of a station so moving on.
            return;

        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        var respawnDest = _stationSystem.GetLargestGridOnStation(component.AttachedStation.Value);

        if (respawnDest is null || Deleted(respawnDest))
        {
            if (component.AlertStaff)
            {
                _chatManager.SendAdminAnnouncement(
                    Loc.GetString("station-respawn-object-no-destination-grid",
                    ("deleted", ToPrettyString(uid)), ("station", ToPrettyString(component.AttachedStation.Value)), ("location", Transform(uid).Coordinates))
                    );
            }

            return;
        }

        var proto = MetaData(uid).EntityPrototype?.ID;
        if (proto is null)
            return; // Can't respawn entities we can't easily clone.

        var respawnLocation = GetRespawnLocation(respawnDest.Value);
        if (respawnLocation is not null)
        {
            var result = Spawn(proto, respawnLocation.Value);
            if (result != EntityUid.Invalid)
            {
                if (component.AlertStaff)
                {
                    // We're done.
                    _chatManager.SendAdminAnnouncement(
                        Loc.GetString("station-respawn-object-success",
                            ("deleted", ToPrettyString(uid)), ("created", ToPrettyString(result)),
                            ("station", ToPrettyString(component.AttachedStation.Value)),
                            ("oldLocation", Transform(uid).Coordinates), ("newLocation", respawnLocation.Value))
                    );
                }

                return;
            }
        }

        if (component.AlertStaff)
        {
            _chatManager.SendAdminAnnouncement(
                Loc.GetString("station-respawn-object-out-of-attempts",
                    ("deleted", ToPrettyString(uid)), ("station", ToPrettyString(component.AttachedStation.Value)),
                    ("location", Transform(uid).Coordinates), ("proto", proto))
            );
        }
    }

    private EntityCoordinates? GetRespawnLocation(EntityUid respawnDest)
    {
        var tiles = Comp<IMapGridComponent>(respawnDest).Grid.GetAllTiles().ToArray();
        for (var i = 0; i < MaximumRespawnAttempts; i++)
        {
            var tile = _random.Pick(tiles);
            if (tile.IsBlockedTurf(false, _lookupSystem) || tile.IsSpace(_tileDefMan))
                continue;
            return tile.GridPosition();
        }

        return null;
    }
}
