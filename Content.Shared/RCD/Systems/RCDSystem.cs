using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Construction;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
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
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    private readonly int _instantConstructionDelay = 0;
    private readonly EntProtoId _instantConstructionFx = "EffectRCDConstruct0";
    private readonly ProtoId<RCDPrototype> _deconstructTileProto = "DeconstructTile";
    private readonly ProtoId<RCDPrototype> _deconstructLatticeProto = "DeconstructLattice";

    private HashSet<EntityUid> _intersectingEntities = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RCDComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RCDComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RCDComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RCDComponent, RCDDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RCDComponent, DoAfterAttemptEvent<RCDDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<RCDComponent, RCDSystemMessage>(OnRCDSystemMessage);
        SubscribeNetworkEvent<RCDConstructionGhostRotationEvent>(OnRCDconstructionGhostRotationEvent);
    }

    #region Event handling

    private void OnMapInit(EntityUid uid, RCDComponent component, MapInitEvent args)
    {
        // On init, set the RCD to its first available recipe
        if (component.AvailablePrototypes.Any())
        {
            component.ProtoId = component.AvailablePrototypes.First();
            UpdateCachedPrototype(uid, component);
            Dirty(uid, component);

            return;
        }

        // The RCD has no valid recipes somehow? Get rid of it
        QueueDel(uid);
    }

    private void OnRCDSystemMessage(EntityUid uid, RCDComponent component, RCDSystemMessage args)
    {
        // Exit if the RCD doesn't actually know the supplied prototype
        if (!component.AvailablePrototypes.Contains(args.ProtoId))
            return;

        if (!_protoManager.HasIndex(args.ProtoId))
            return;

        // Set the current RCD prototype to the one supplied
        component.ProtoId = args.ProtoId;
        UpdateCachedPrototype(uid, component);
        Dirty(uid, component);
    }

    private void OnExamine(EntityUid uid, RCDComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // Update cached prototype if required
        UpdateCachedPrototype(uid, component);

        var msg = Loc.GetString("rcd-component-examine-mode-details", ("mode", Loc.GetString(component.CachedPrototype.SetName)));

        if (component.CachedPrototype.Mode == RcdMode.ConstructTile || component.CachedPrototype.Mode == RcdMode.ConstructObject)
        {
            var name = Loc.GetString(component.CachedPrototype.SetName);

            if (component.CachedPrototype.Prototype != null &&
                _protoManager.TryIndex(component.CachedPrototype.Prototype, out var proto))
                name = proto.Name;

            msg = Loc.GetString("rcd-component-examine-build-details", ("name", name));
        }

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

        if (!TryGetMapGridData(location, out var mapGridData))
        {
            _popup.PopupClient(Loc.GetString("rcd-component-no-valid-grid"), uid, user);
            return;
        }

        if (!IsRCDOperationStillValid(uid, component, mapGridData.Value, args.Target, args.User))
            return;

        if (!_net.IsServer)
            return;

        // Get the starting cost, delay, and effect from the prototype
        var cost = component.CachedPrototype.Cost;
        var delay = component.CachedPrototype.Delay;
        var effectPrototype = component.CachedPrototype.Effect;

        #region: Operation modifiers

        // Deconstruction modifiers
        switch (component.CachedPrototype.Mode)
        {
            case RcdMode.Deconstruct:

                // Deconstructing an object
                if (args.Target != null)
                {
                    if (TryComp<RCDDeconstructableComponent>(args.Target, out var destructible))
                    {
                        cost = destructible.Cost;
                        delay = destructible.Delay;
                        effectPrototype = destructible.Effect;
                    }
                }

                // Deconstructing a tile
                else
                {
                    var deconstructedTile = _mapSystem.GetTileRef(mapGridData.Value.GridUid, mapGridData.Value.Component, mapGridData.Value.Location);
                    var protoName = !deconstructedTile.IsSpace() ? _deconstructTileProto : _deconstructLatticeProto;

                    if (_protoManager.TryIndex(protoName, out var deconProto))
                    {
                        cost = deconProto.Cost;
                        delay = deconProto.Delay;
                        effectPrototype = deconProto.Effect;
                    }
                }

                break;

            case RcdMode.ConstructTile:

                // If replacing a tile, make the construction instant
                var contructedTile = _mapSystem.GetTileRef(mapGridData.Value.GridUid, mapGridData.Value.Component, mapGridData.Value.Location);

                if (!contructedTile.Tile.IsEmpty)
                {
                    delay = _instantConstructionDelay;
                    effectPrototype = _instantConstructionFx;
                }

                break;
        }

        #endregion

        // Try to start the do after
        var effect = Spawn(effectPrototype, mapGridData.Value.Location);
        var ev = new RCDDoAfterEvent(GetNetCoordinates(mapGridData.Value.Location), component.ConstructionDirection, component.ProtoId, cost, EntityManager.GetNetEntity(effect));

        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, ev, uid, target: args.Target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            CancelDuplicate = false,
            BlockDuplicate = false
        };

        args.Handled = true;

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            QueueDel(effect);
    }

    private void OnDoAfterAttempt(EntityUid uid, RCDComponent component, DoAfterAttemptEvent<RCDDoAfterEvent> args)
    {
        if (args.Event?.DoAfter?.Args == null)
            return;

        // Exit if the RCD prototype has changed
        if (component.ProtoId != args.Event.StartingProtoId)
        {
            args.Cancel();
            return;
        }

        // Ensure the RCD operation is still valid
        var location = GetCoordinates(args.Event.Location);

        if (!TryGetMapGridData(location, out var mapGridData))
        {
            args.Cancel();
            return;
        }

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
        FinalizeRCDOperation(uid, component, mapGridData.Value, args.Direction, args.Target, args.User);

        // Play audio and consume charges
        _audio.PlayPredicted(component.SuccessSound, uid, args.User);
        _charges.UseCharges(uid, args.Cost);
    }

    private void OnRCDconstructionGhostRotationEvent(RCDConstructionGhostRotationEvent ev, EntitySessionEventArgs session)
    {
        var uid = GetEntity(ev.NetEntity);

        // Determine if player that send the message is carrying the specified RCD in their active hand
        if (session.SenderSession.AttachedEntity == null)
            return;

        if (!TryComp<HandsComponent>(session.SenderSession.AttachedEntity, out var hands) ||
            uid != hands.ActiveHand?.HeldEntity)
            return;

        if (!TryComp<RCDComponent>(uid, out var rcd))
            return;

        // Update the construction direction
        rcd.ConstructionDirection = ev.Direction;
        Dirty(uid, rcd);
    }

    #endregion

    #region Entity construction/deconstruction rule checks

    public bool IsRCDOperationStillValid(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid? target, EntityUid user, bool popMsgs = true)
    {
        // Update cached prototype if required
        UpdateCachedPrototype(uid, component);

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
        var unobstructed = (target == null)
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
                _popup.PopupClient(Loc.GetString("rcd-component-must-build-on-empty-tile-message"), uid, user);

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
                _popup.PopupClient(Loc.GetString("rcd-component-must-build-on-subfloor-message"), uid, user);

            return false;
        }

        // Tile specific rules
        if (component.CachedPrototype.Mode == RcdMode.ConstructTile)
        {
            // Check rule: Tile placement is valid
            if (!_floors.CanPlaceTile(mapGridData.GridUid, mapGridData.Component, out var reason))
            {
                if (popMsgs)
                    _popup.PopupClient(reason, uid, user);

                return false;
            }

            // Check rule: Tiles can't be identical
            if (mapGridData.Tile.Tile.GetContentTileDefinition().ID == component.CachedPrototype.Prototype)
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-identical-tile"), uid, user);

                return false;
            }

            // Ensure that all construction rules shared between tiles and object are checked before exiting here
            return true;
        }

        // Entity specific rules

        // Check rule: The tile is unoccupied
        var isWindow = component.CachedPrototype.ConstructionRules.Contains(RcdConstructionRule.IsWindow);
        var isCatwalk = component.CachedPrototype.ConstructionRules.Contains(RcdConstructionRule.IsCatwalk);

        _intersectingEntities.Clear();
        _lookup.GetLocalEntitiesIntersecting(mapGridData.GridUid, mapGridData.Position, _intersectingEntities, -0.05f, LookupFlags.Uncontained);

        foreach (var ent in _intersectingEntities)
        {
            if (isWindow && HasComp<SharedCanBuildWindowOnTopComponent>(ent))
                continue;

            if (isCatwalk && _tags.HasTag(ent, "Catwalk"))
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-on-occupied-tile-message"), uid, user);

                return false;
            }

            if (component.CachedPrototype.CollisionMask != CollisionGroup.None && TryComp<FixturesComponent>(ent, out var fixtures))
            {
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    // Continue if no collision is possible
                    if (!fixture.Hard || fixture.CollisionLayer <= 0 || (fixture.CollisionLayer & (int) component.CachedPrototype.CollisionMask) == 0)
                        continue;

                    // Continue if our custom collision bounds are not intersected
                    if (component.CachedPrototype.CollisionPolygon != null &&
                        !DoesCustomBoundsIntersectWithFixture(component.CachedPrototype.CollisionPolygon, component.ConstructionTransform, ent, fixture))
                        continue;

                    // Collision was detected
                    if (popMsgs)
                        _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-on-occupied-tile-message"), uid, user);

                    return false;
                }
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
            if (!TryComp<RCDDeconstructableComponent>(target, out var deconstructible) || !deconstructible.Deconstructable)
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

    private void FinalizeRCDOperation(EntityUid uid, RCDComponent component, MapGridData mapGridData, Direction direction, EntityUid? target, EntityUid user)
    {
        if (!_net.IsServer)
            return;

        if (component.CachedPrototype.Prototype == null)
            return;

        switch (component.CachedPrototype.Mode)
        {
            case RcdMode.ConstructTile:
                _mapSystem.SetTile(mapGridData.GridUid, mapGridData.Component, mapGridData.Position, new Tile(_tileDefMan[component.CachedPrototype.Prototype].TileId));
                _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to set grid: {mapGridData.GridUid} {mapGridData.Position} to {component.CachedPrototype.Prototype}");
                break;

            case RcdMode.ConstructObject:
                var ent = Spawn(component.CachedPrototype.Prototype, _mapSystem.GridTileToLocal(mapGridData.GridUid, mapGridData.Component, mapGridData.Position));

                switch (component.CachedPrototype.Rotation)
                {
                    case RcdRotation.Fixed:
                        Transform(ent).LocalRotation = Angle.Zero;
                        break;
                    case RcdRotation.Camera:
                        Transform(ent).LocalRotation = Transform(uid).LocalRotation;
                        break;
                    case RcdRotation.User:
                        Transform(ent).LocalRotation = direction.ToAngle();
                        break;
                }

                _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to spawn {ToPrettyString(ent)} at {mapGridData.Position} on grid {mapGridData.GridUid}");
                break;

            case RcdMode.Deconstruct:

                if (target == null)
                {
                    // Deconstruct tile (either converts the tile to lattice, or removes lattice)
                    var tile = (mapGridData.Tile.Tile.GetContentTileDefinition().ID != "Lattice") ? new Tile(_tileDefMan["Lattice"].TileId) : Tile.Empty;
                    _mapSystem.SetTile(mapGridData.GridUid, mapGridData.Component, mapGridData.Position, tile);
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to set grid: {mapGridData.GridUid} tile: {mapGridData.Position} open to space");
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

    #region Utility functions

    public bool TryGetMapGridData(EntityCoordinates location, [NotNullWhen(true)] out MapGridData? mapGridData)
    {
        mapGridData = null;
        var gridUid = location.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            location = location.AlignWithClosestGridTile(1.75f, EntityManager);
            gridUid = location.GetGridUid(EntityManager);

            // Check if we got a grid ID the second time round
            if (!TryComp(gridUid, out mapGrid))
                return false;
        }

        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);
        mapGridData = new MapGridData(gridUid.Value, mapGrid, location, tile, position);

        return true;
    }

    private bool DoesCustomBoundsIntersectWithFixture(PolygonShape boundingPolygon, Transform boundingTransform, EntityUid fixtureOwner, Fixture fixture)
    {
        var entXformComp = Transform(fixtureOwner);
        var entXform = new Transform(new(), entXformComp.LocalRotation);

        return boundingPolygon.ComputeAABB(boundingTransform, 0).Intersects(fixture.Shape.ComputeAABB(entXform, 0));
    }

    public void UpdateCachedPrototype(EntityUid uid, RCDComponent component)
    {
        if (component.ProtoId.Id != component.CachedPrototype?.Prototype)
            component.CachedPrototype = _protoManager.Index(component.ProtoId);
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
    [DataField(required: true)]
    public NetCoordinates Location { get; private set; } = default!;

    [DataField]
    public Direction Direction { get; private set; } = default!;

    [DataField]
    public ProtoId<RCDPrototype> StartingProtoId { get; private set; } = default!;

    [DataField]
    public int Cost { get; private set; } = 1;

    [DataField("fx")]
    public NetEntity? Effect { get; private set; } = null;

    private RCDDoAfterEvent() { }

    public RCDDoAfterEvent(NetCoordinates location, Direction direction, ProtoId<RCDPrototype> startingProtoId, int cost, NetEntity? effect = null)
    {
        Location = location;
        Direction = direction;
        StartingProtoId = startingProtoId;
        Cost = cost;
        Effect = effect;
    }

    public override DoAfterEvent Clone() => this;
}
