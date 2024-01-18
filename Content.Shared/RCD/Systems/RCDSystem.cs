using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Construction;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.RCD.Components;
using Content.Shared.Tag;
using Content.Shared.Tiles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.RCD.Systems;

[Virtual]
public class RCDSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
    [Dependency] private readonly FloorTileSystem _floors = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RCDComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RCDComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RCDComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RCDComponent, RCDDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RCDComponent, DoAfterAttemptEvent<RCDDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<RCDComponent, RCDSystemMessage>(OnRCDSystemMessage);
        SubscribeNetworkEvent<RCDConstructionGhostRotationEvent>(OnRCDconstructionGhostRotationEvent);
    }

    #region Event handling

    private void OnInit(EntityUid uid, RCDComponent component, ComponentInit args)
    {
        // On init, set the RCD to its first available recipe
        foreach (var protoId in component.AvailablePrototypes)
        {
            var proto = _protoManager.Index(protoId);

            if (proto != null)
            {
                component.ProtoId = protoId;
                component.CachedPrototype = proto;
                Dirty(uid, component);

                return;
            }
        }

        // The RCD has no valid recipes somehow? Get rid of it
        QueueDel(uid);
    }

    private void OnRCDSystemMessage(EntityUid uid, RCDComponent component, RCDSystemMessage args)
    {
        // Exit if the RCD doesn't actually know the supplied prototype
        if (!component.AvailablePrototypes.Contains(args.ProtoId))
            return;

        if (!_protoManager.TryIndex(args.ProtoId, out var proto))
            return;

        // Update the current RCD prototype to the one supplied
        component.ProtoId = args.ProtoId;
        component.CachedPrototype = proto;
        Dirty(uid, component);

        if (args.Session.AttachedEntity != null)
        {
            // Popup message
            var msg = (component.CachedPrototype.Prototype != null) ?
                Loc.GetString("rcd-component-change-build-mode", ("name", Loc.GetString(component.CachedPrototype.SetName))) :
                Loc.GetString("rcd-component-change-mode", ("mode", Loc.GetString(component.CachedPrototype.SetName)));

            _popup.PopupEntity(msg, uid, args.Session.AttachedEntity.Value);
        }
    }

    private void OnExamine(EntityUid uid, RCDComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var msg = (component.CachedPrototype.Prototype != null) ?
            Loc.GetString("rcd-component-examine-build-details", ("name", Loc.GetString(component.CachedPrototype.SetName))) :
            Loc.GetString("rcd-component-examine-mode-details", ("mode", Loc.GetString(component.CachedPrototype.SetName)));

        args.PushMarkup(msg);
    }

    private void OnAfterInteract(EntityUid uid, RCDComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        var user = args.User;
        var location = args.ClickLocation;

        // Initial validity checks
        if (!location.IsValid(EntityManager))
            return;

        var gridUid = location.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            location = location.AlignWithClosestGridTile();
            gridUid = location.GetGridUid(EntityManager);

            // Check if fixing it failed / get final grid ID
            if (!TryComp<MapGridComponent>(gridUid, out mapGrid))
                return;
        }

        if (!TryGetMapGridData(location, out var mapGridData))
            return;

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (!IsRCDOperationStillValid(uid, component, mapGridData.Value, args.Target, args.User))
            return;

        if (!_net.IsServer)
            return;

        // If not placing a tile on empty space, make the construction instant
        var delay = component.CachedPrototype.Delay;
        var effectPrototype = component.CachedPrototype.Effect;
        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);

        if (component.CachedPrototype.Mode == RcdMode.ConstructTile && !tile.Tile.IsEmpty)
        {
            delay = 0;
            effectPrototype = "EffectRCDConstructInstant";
        }

        // Try to start the do after
        var effect = Spawn(effectPrototype, location);
        var ev = new RCDDoAfterEvent(GetNetCoordinates(location), component.ProtoId, EntityManager.GetNetEntity(effect));

        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, ev, uid, target: args.Target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnUserMove = true,
            BreakOnTargetMove = args.Target != null,
            AttemptFrequency = AttemptFrequency.EveryTick,
            CancelDuplicate = false,
            BlockDuplicate = false
        };

        args.Handled = true;

        if (!_doAfter.TryStartDoAfter(doAfterArgs) && _net.IsServer)
            QueueDel(effect);
    }

    private void OnDoAfterAttempt(EntityUid uid, RCDComponent component, DoAfterAttemptEvent<RCDDoAfterEvent> args)
    {
        if (args.Event?.DoAfter?.Args == null)
            return;

        // Exit if the RCD prototype has changed
        if (component.ProtoId != args.Event.StartingProtoId)
            return;

        // Ensure the RCD operation is still valid
        var location = GetCoordinates(args.Event.Location);

        if (!TryGetMapGridData(location, out var mapGridData))
            return;

        if (!IsRCDOperationStillValid(uid, component, mapGridData.Value, args.Event.Target, args.Event.User))
            args.Cancel();
    }

    private void OnDoAfter(EntityUid uid, RCDComponent component, RCDDoAfterEvent args)
    {
        if (args.Cancelled && _net.IsServer)
            QueueDel(EntityManager.GetEntity(args.Effect));

        if (args.Handled || args.Cancelled || !_timing.IsFirstTimePredicted)
            return;

        args.Handled = true;

        var location = GetCoordinates(args.Location);

        if (!TryGetMapGridData(location, out var mapGridData))
            return;

        // Ensure the RCD operation is still valid
        if (!IsRCDOperationStillValid(uid, component, mapGridData.Value, args.Target, args.User))
            return;

        // Finalize the operation
        FinalizeRCDOperation(uid, component, mapGridData.Value, args.Target, args.User);

        // Play audio and consume charges
        _audio.PlayPredicted(component.SuccessSound, uid, args.User);
        _charges.UseCharges(uid, component.CachedPrototype.Cost);
    }

    private void OnRCDconstructionGhostRotationEvent(RCDConstructionGhostRotationEvent ev)
    {
        var uid = GetEntity(ev.NetEntity);

        if (!TryComp<RCDComponent>(uid, out var rcd))
            return;

        rcd.ConstructionDirection = ev.Direction;
        Dirty(uid, rcd);
    }

    #endregion

    #region Entity construction/deconstruction rule checks

    public bool IsRCDOperationStillValid(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid? target, EntityUid user, bool popMsgs = true)
    {
        // Update cached prototype if required (should only be needed by the client, for prediction)
        if (component.ProtoId.Id != component.CachedPrototype.Prototype)
            component.CachedPrototype = _protoManager.Index(component.ProtoId);

        // Check that the RCD has enough ammo to get the job done
        TryComp<LimitedChargesComponent>(uid, out var charges);

        // Both of these were messages were suppose to be predicted, but HasInsufficientCharges wasn't being checked on the client for some reason?
        if (_charges.IsEmpty(uid, charges))
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcd-component-no-ammo-message"), uid, user);

            return false;
        }

        if (_charges.HasInsufficientCharges(uid, component.CachedPrototype.Cost, charges))
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcd-component-insufficient-ammo-message"), uid, user);

            return false;
        }

        // Exit if the target / target location is obstructed
        var unobstructed = target == null
            ? _interaction.InRangeUnobstructed(user, _mapSystem.GridTileToWorld(mapGridData.GridUid, mapGridData.Component, mapGridData.Position), popup: popMsgs)
            : _interaction.InRangeUnobstructed(user, target.Value, popup: popMsgs);

        if (!unobstructed)
            return false;

        // Return whether the operation location is valid
        switch (component.CachedPrototype.Mode)
        {
            case RcdMode.ConstructTile: return IsConstructionLocationValid(uid, component, mapGridData, user, popMsgs);
            case RcdMode.ConstructObject: return IsConstructionLocationValid(uid, component, mapGridData, user, popMsgs);
            case RcdMode.Deconstruct: return IsDeconstructionStillValid(uid, component, mapGridData, target, user, popMsgs);
        }

        return false;
    }

    private bool IsConstructionLocationValid(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid user, bool popMsgs = true)
    {
        // Check rule: Must build on empty tile
        if (component.CachedPrototype.ConstructionRules.Contains(RcdConstructionRule.MustBuildOnEmptyTile) && !mapGridData.Tile.Tile.IsEmpty)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-on-non-empty-tile-message"), uid, user);

            return false;
        }

        // Check rule: Must build on non-empty tile
        if (!component.CachedPrototype.ConstructionRules.Contains(RcdConstructionRule.CanBuildOnEmptyTile) && mapGridData.Tile.Tile.IsEmpty)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-on-empty-tile-message"), uid, user);

            return false;
        }

        // Check rule: Must place on subfloor
        if (component.CachedPrototype.ConstructionRules.Contains(RcdConstructionRule.MustBuildOnSubfloor) && !mapGridData.Tile.Tile.GetContentTileDefinition().IsSubFloor)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-as-subfloor-not-exposed-message"), uid, user);

            return false;
        }

        // Tile specific rules
        if (component.CachedPrototype.Mode == RcdMode.ConstructTile)
        {
            // Check rule: Tile placement is valid
            if (!_floors.CanPlaceTile(mapGridData.GridUid, mapGridData.Component, out var reason))
            {
                if (popMsgs)
                    _popup.PopupClient(reason, user, user);

                return false;
            }

            // Ensure that all construction rules shared between tiles and object are checked before exiting here
            return true;
        }

        // Check rule: The tile is unoccupied
        bool isWindow = component.CachedPrototype.ConstructionRules.Contains(RcdConstructionRule.IsWindow);

        foreach (var ent in _lookup.GetEntitiesIntersecting(mapGridData.Tile, -0.1f, LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Static))
        {
            if (!TryComp<FixturesComponent>(ent, out var fixtures))
                continue;

            if (isWindow && HasComp<SharedCanBuildWindowOnTopComponent>(ent))
                continue;

            for (int i = 0; i < fixtures.FixtureCount; i++)
            {
                (var _, var fixture) = fixtures.Fixtures.ElementAt(i);

                if ((fixture.CollisionLayer & (int) component.CachedPrototype.CollisionMask) == 0)
                    continue;

                if (component.CachedPrototype.ConstructionRules.Contains(RcdConstructionRule.OnePerCardinalDirection) &&
                    Prototype(ent)?.ID == component.CachedPrototype.Prototype &&
                    component.ConstructionDirection != Transform(ent).LocalRotation.GetCardinalDir())
                    continue;

                // Collision detected
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-as-space-is-occupied-message"), uid, user);

                return false;
            }
        }

        return true;
    }

    private bool IsDeconstructionStillValid(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid? target, EntityUid user, bool popMsgs = true)
    {
        // Attempt to deconstruct a floor tile
        if (target == null)
        {
            // The tile is empty
            if (mapGridData.Tile.Tile.IsEmpty)
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcd-component-nothing-to-deconstruct-message"), uid, user);

                return false;
            }

            // The tile has a structure sitting on it
            if (_turf.IsTileBlocked(mapGridData.Tile, CollisionGroup.MobMask))
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcd-component-tile-obstructed-message"), uid, user);

                return false;
            }

            // The tile cannot be destroyed
            var tileDef = (ContentTileDefinition) _tileDefMan[mapGridData.Tile.Tile.TypeId];
            if (tileDef.Indestructible)
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcd-component-tile-indestructible-message"), uid, user);

                return false;
            }
        }

        // Attempt to deconstruct an object
        else
        {
            // The object is not in the whitelist
            if (!_tag.HasTag(target.Value, "RCDDeconstructWhitelist"))
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcd-component-deconstruct-target-not-on-whitelist-message"), uid, user);

                return false;
            }
        }

        return true;
    }

    #endregion

    #region Entity construction/deconstruction

    private void FinalizeRCDOperation(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid? target, EntityUid user)
    {
        if (!_net.IsServer)
            return;

        switch (component.CachedPrototype.Mode)
        {
            case RcdMode.ConstructTile:
                _mapSystem.SetTile(mapGridData.GridUid, mapGridData.Component, mapGridData.Position, new Tile(_tileDefMan[component.CachedPrototype.Prototype!].TileId));
                _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to set grid: {mapGridData.GridUid} {mapGridData.Position} to {component.CachedPrototype.Prototype}");
                break;

            case RcdMode.ConstructObject:
                var ent = Spawn(component.CachedPrototype.Prototype!, _mapSystem.GridTileToLocal(mapGridData.GridUid, mapGridData.Component, mapGridData.Position));

                switch (component.CachedPrototype.Rotation)
                {
                    case RcdRotation.Fixed:
                        Transform(ent).LocalRotation = Angle.Zero;
                        break;
                    case RcdRotation.Camera:
                        Transform(ent).LocalRotation = Transform(uid).LocalRotation;
                        break;
                    case RcdRotation.User:
                        Transform(ent).LocalRotation = component.ConstructionDirection.ToAngle();
                        break;
                }

                _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to spawn {ToPrettyString(ent)} at {mapGridData.Position} on grid {mapGridData.GridUid}");
                break;

            case RcdMode.Deconstruct:

                if (target == null)
                {
                    // Deconstruct tile
                    _mapSystem.SetTile(mapGridData.GridUid, mapGridData.Component, mapGridData.Position, Tile.Empty);
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to set grid: {mapGridData.GridUid} tile: {mapGridData.Position} to space");
                }
                else
                {
                    // Deconstruct object
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to delete {ToPrettyString(target):target}");
                    QueueDel(target);
                }

                break;
        }
    }

    #endregion

    #region Data retrieval functions

    private bool TryGetMapGrid(EntityUid? gridUid, EntityCoordinates location, [NotNullWhen(true)] out MapGridComponent? mapGrid)
    {
        if (!TryComp(gridUid, out mapGrid))
        {
            location = location.AlignWithClosestGridTile();
            gridUid = location.GetGridUid(EntityManager);

            // Check if updating the location resulted in a grid being found
            if (!TryComp(gridUid, out mapGrid))
                return false;
        }

        return true;
    }

    public bool TryGetMapGridData(EntityCoordinates location, [NotNullWhen(true)] out MapGridData? mapGridData)
    {
        mapGridData = null;
        var gridUid = location.GetGridUid(EntityManager);

        if (!TryGetMapGrid(gridUid, location, out var mapGrid))
            return false;

        gridUid = mapGrid.Owner;

        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);
        mapGridData = new MapGridData(gridUid.Value, mapGrid, location, tile, position);

        return true;
    }

    #endregion
}

public struct MapGridData
{
    public EntityUid GridUid;
    public MapGridComponent Component;
    public EntityCoordinates Location;
    public TileRef Tile;
    public Vector2i Position;

    public MapGridData(EntityUid gridUid, MapGridComponent component, EntityCoordinates location, TileRef tile, Vector2i position)
    {
        GridUid = gridUid;
        Component = component;
        Location = location;
        Tile = tile;
        Position = position;
    }
}

[Serializable, NetSerializable]
public sealed partial class RCDDoAfterEvent : DoAfterEvent
{
    [DataField("location", required: true)]
    public NetCoordinates Location { get; private set; } = default!;

    [DataField("startingProtoId")]
    public ProtoId<RCDPrototype> StartingProtoId { get; private set; } = default!;

    [DataField("fx")]
    public NetEntity? Effect { get; private set; } = null;

    private RCDDoAfterEvent() { }

    public RCDDoAfterEvent(NetCoordinates location, ProtoId<RCDPrototype> startingProtoId, NetEntity? effect = null)
    {
        Location = location;
        StartingProtoId = startingProtoId;
        Effect = effect;
    }

    public override DoAfterEvent Clone() => this;
}
