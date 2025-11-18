using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Systems;
using Content.Shared.Construction;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
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
using System.Linq;

namespace Content.Shared.RCD.Systems;

public sealed class RCDSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
    [Dependency] private readonly FloorTileSystem _floors = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _sharedCharges = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    private readonly int _instantConstructionDelay = 0;
    private readonly EntProtoId _instantConstructionFx = "EffectRCDConstruct0";
    private readonly ProtoId<RCDPrototype> _deconstructTileProto = "DeconstructTile";
    private readonly ProtoId<RCDPrototype> _deconstructLatticeProto = "DeconstructLattice";
    private static readonly ProtoId<TagPrototype> CatwalkTag = "Catwalk";

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
        if (component.AvailablePrototypes.Count > 0)
        {
            component.ProtoId = component.AvailablePrototypes.ElementAt(0);
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

        if (!_protoManager.Resolve<RCDPrototype>(args.ProtoId, out var prototype))
            return;

        // Set the current RCD prototype to the one supplied
        component.ProtoId = args.ProtoId;

        _adminLogger.Add(LogType.RCD, LogImpact.Low, $"{args.Actor} set RCD mode to: {prototype.Mode} : {prototype.Prototype}");

        Dirty(uid, component);
    }

    private void OnExamine(EntityUid uid, RCDComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var prototype = _protoManager.Index(component.ProtoId);

        var msg = Loc.GetString("rcd-component-examine-mode-details", ("mode", Loc.GetString(prototype.SetName)));

        if (prototype.Mode == RcdMode.ConstructTile || prototype.Mode == RcdMode.ConstructObject)
        {
            var name = Loc.GetString(prototype.SetName);

            if (prototype.Prototype != null &&
                _protoManager.TryIndex(prototype.Prototype, out var proto)) // don't use Resolve because this can be a tile
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
        var prototype = _protoManager.Index(component.ProtoId);

        // Initial validity checks
        if (!location.IsValid(EntityManager))
            return;

        var gridUid = _transform.GetGrid(location);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            _popup.PopupClient(Loc.GetString("rcd-component-no-valid-grid"), uid, user);
            return;
        }
        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);

        if (!IsRCDOperationStillValid(uid, component, gridUid.Value, mapGrid, tile, position, args.Target, args.User))
            return;

        if (!_net.IsServer)
            return;

        // Get the starting cost, delay, and effect from the prototype
        var cost = prototype.Cost;
        var delay = prototype.Delay;
        var effectPrototype = prototype.Effect;

        #region: Operation modifiers

        // Deconstruction modifiers
        switch (prototype.Mode)
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
                    var deconstructedTile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
                    var protoName = !_turf.IsSpace(deconstructedTile) ? _deconstructTileProto : _deconstructLatticeProto;

                    if (_protoManager.Resolve(protoName, out var deconProto))
                    {
                        cost = deconProto.Cost;
                        delay = deconProto.Delay;
                        effectPrototype = deconProto.Effect;
                    }
                }

                break;

            case RcdMode.ConstructTile:

                // If replacing a tile, make the construction instant
                var contructedTile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);

                if (!contructedTile.Tile.IsEmpty)
                {
                    delay = _instantConstructionDelay;
                    effectPrototype = _instantConstructionFx;
                }

                break;
        }

        #endregion

        // Try to start the do after
        var effect = Spawn(effectPrototype, location);
        var ev = new RCDDoAfterEvent(GetNetCoordinates(location), component.ConstructionDirection, component.ProtoId, cost, GetNetEntity(effect));

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

        var gridUid = _transform.GetGrid(location);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            args.Cancel();
            return;
        }


        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);

        if (!IsRCDOperationStillValid(uid, component, gridUid.Value, mapGrid, tile, position, args.Event.Target, args.Event.User))
            args.Cancel();
    }

    private void OnDoAfter(EntityUid uid, RCDComponent component, RCDDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            // Delete the effect entity if the do-after was cancelled (server-side only)
            if (_net.IsServer)
                QueueDel(GetEntity(args.Effect));
            return;
        }

        if (args.Handled)
            return;

        args.Handled = true;

        var location = GetCoordinates(args.Location);

        var gridUid = _transform.GetGrid(location);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);

        // Ensure the RCD operation is still valid
        if (!IsRCDOperationStillValid(uid, component, gridUid.Value, mapGrid, tile, position, args.Target, args.User))
            return;

        // Finalize the operation (this should handle prediction properly)
        FinalizeRCDOperation(uid, component, gridUid.Value, mapGrid, tile, position, args.Direction, args.Target, args.User);

        // Play audio and consume charges
        _audio.PlayPredicted(component.SuccessSound, uid, args.User);
        _sharedCharges.AddCharges(uid, -args.Cost);
    }

    private void OnRCDconstructionGhostRotationEvent(RCDConstructionGhostRotationEvent ev, EntitySessionEventArgs session)
    {
        var uid = GetEntity(ev.NetEntity);

        // Determine if player that send the message is carrying the specified RCD in their active hand
        if (session.SenderSession.AttachedEntity is not { } player)
            return;

        if (_hands.GetActiveItem(player) != uid)
            return;

        if (!TryComp<RCDComponent>(uid, out var rcd))
            return;

        // Update the construction direction
        rcd.ConstructionDirection = ev.Direction;
        Dirty(uid, rcd);
    }

    #endregion

    #region Entity construction/deconstruction rule checks

    public bool IsRCDOperationStillValid(EntityUid uid, RCDComponent component, EntityUid gridUid, MapGridComponent mapGrid, TileRef tile, Vector2i position, EntityUid? target, EntityUid user, bool popMsgs = true)
    {
        var prototype = _protoManager.Index(component.ProtoId);

        // Check that the RCD has enough ammo to get the job done
        var charges = _sharedCharges.GetCurrentCharges(uid);

        // Both of these were messages were suppose to be predicted, but HasInsufficientCharges wasn't being checked on the client for some reason?
        if (charges == 0)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcd-component-no-ammo-message"), uid, user);

            return false;
        }

        if (prototype.Cost > charges)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcd-component-insufficient-ammo-message"), uid, user);

            return false;
        }

        // Exit if the target / target location is obstructed
        var unobstructed = (target == null)
            ? _interaction.InRangeUnobstructed(user, _mapSystem.GridTileToWorld(gridUid, mapGrid, position), popup: popMsgs)
            : _interaction.InRangeUnobstructed(user, target.Value, popup: popMsgs);

        if (!unobstructed)
            return false;

        // Return whether the operation location is valid
        switch (prototype.Mode)
        {
            case RcdMode.ConstructTile:
            case RcdMode.ConstructObject:
                return IsConstructionLocationValid(uid, component, gridUid, mapGrid, tile, position, user, popMsgs);
            case RcdMode.Deconstruct:
                return IsDeconstructionStillValid(uid, tile, target, user, popMsgs);
        }

        return false;
    }

    private bool IsConstructionLocationValid(EntityUid uid, RCDComponent component, EntityUid gridUid, MapGridComponent mapGrid, TileRef tile, Vector2i position, EntityUid user, bool popMsgs = true)
    {
        var prototype = _protoManager.Index(component.ProtoId);

        // Check rule: Must build on empty tile
        if (prototype.ConstructionRules.Contains(RcdConstructionRule.MustBuildOnEmptyTile) && !tile.Tile.IsEmpty)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcd-component-must-build-on-empty-tile-message"), uid, user);

            return false;
        }

        // Check rule: Must build on non-empty tile
        if (!prototype.ConstructionRules.Contains(RcdConstructionRule.CanBuildOnEmptyTile) && tile.Tile.IsEmpty)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-on-empty-tile-message"), uid, user);

            return false;
        }

        // Check rule: Must place on subfloor
        if (prototype.ConstructionRules.Contains(RcdConstructionRule.MustBuildOnSubfloor) && !_turf.GetContentTileDefinition(tile).IsSubFloor)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcd-component-must-build-on-subfloor-message"), uid, user);

            return false;
        }

        // Tile specific rules
        if (prototype.Mode == RcdMode.ConstructTile)
        {
            // Check rule: Tile placement is valid
            if (!_floors.CanPlaceTile(gridUid, mapGrid, tile.GridIndices, out var reason))
            {
                if (popMsgs)
                    _popup.PopupClient(reason, uid, user);

                return false;
            }

            // Check rule: Tiles can't be identical
            if (_turf.GetContentTileDefinition(tile).ID == prototype.Prototype)
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
        var isWindow = prototype.ConstructionRules.Contains(RcdConstructionRule.IsWindow);
        var isCatwalk = prototype.ConstructionRules.Contains(RcdConstructionRule.IsCatwalk);

        _intersectingEntities.Clear();
        _lookup.GetLocalEntitiesIntersecting(gridUid, position, _intersectingEntities, -0.05f, LookupFlags.Uncontained);

        foreach (var ent in _intersectingEntities)
        {
            if (isWindow && HasComp<SharedCanBuildWindowOnTopComponent>(ent))
                continue;

            if (isCatwalk && _tags.HasTag(ent, CatwalkTag))
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-on-occupied-tile-message"), uid, user);

                return false;
            }

            if (prototype.CollisionMask != CollisionGroup.None && TryComp<FixturesComponent>(ent, out var fixtures))
            {
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    // Continue if no collision is possible
                    if (!fixture.Hard || fixture.CollisionLayer <= 0 || (fixture.CollisionLayer & (int) prototype.CollisionMask) == 0)
                        continue;

                    // Continue if our custom collision bounds are not intersected
                    if (prototype.CollisionPolygon != null &&
                        !DoesCustomBoundsIntersectWithFixture(prototype.CollisionPolygon, component.ConstructionTransform, ent, fixture))
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

    private bool IsDeconstructionStillValid(EntityUid uid, TileRef tile, EntityUid? target, EntityUid user, bool popMsgs = true)
    {
        // Attempt to deconstruct a floor tile
        if (target == null)
        {
            // The tile is empty
            if (tile.Tile.IsEmpty)
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcd-component-nothing-to-deconstruct-message"), uid, user);

                return false;
            }

            // The tile has a structure sitting on it
            if (_turf.IsTileBlocked(tile, CollisionGroup.MobMask))
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcd-component-tile-obstructed-message"), uid, user);

                return false;
            }

            // The tile cannot be destroyed
            var tileDef = _turf.GetContentTileDefinition(tile);

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

    private void FinalizeRCDOperation(EntityUid uid, RCDComponent component, EntityUid gridUid, MapGridComponent mapGrid, TileRef tile, Vector2i position, Direction direction, EntityUid? target, EntityUid user)
    {
        if (!_net.IsServer)
            return;

        var prototype = _protoManager.Index(component.ProtoId);

        if (prototype.Prototype == null)
            return;

        switch (prototype.Mode)
        {
            case RcdMode.ConstructTile:
                _mapSystem.SetTile(gridUid, mapGrid, position, new Tile(_tileDefMan[prototype.Prototype].TileId));
                _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to set grid: {gridUid} {position} to {prototype.Prototype}");
                break;

            case RcdMode.ConstructObject:
                var ent = Spawn(prototype.Prototype, _mapSystem.GridTileToLocal(gridUid, mapGrid, position));

                switch (prototype.Rotation)
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

                _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to spawn {ToPrettyString(ent)} at {position} on grid {gridUid}");
                break;

            case RcdMode.Deconstruct:

                if (target == null)
                {
                    // Deconstruct tile (either converts the tile to lattice, or removes lattice)
                    var tileDef = (_turf.GetContentTileDefinition(tile).ID != "Lattice") ? new Tile(_tileDefMan["Lattice"].TileId) : Tile.Empty;
                    _mapSystem.SetTile(gridUid, mapGrid, position, tileDef);
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to set grid: {gridUid} tile: {position} open to space");
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

    private bool DoesCustomBoundsIntersectWithFixture(PolygonShape boundingPolygon, Transform boundingTransform, EntityUid fixtureOwner, Fixture fixture)
    {
        var entXformComp = Transform(fixtureOwner);
        var entXform = new Transform(new(), entXformComp.LocalRotation);

        return boundingPolygon.ComputeAABB(boundingTransform, 0).Intersects(fixture.Shape.ComputeAABB(entXform, 0));
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed partial class RCDDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public NetCoordinates Location { get; private set; }

    [DataField]
    public Direction Direction { get; private set; }

    [DataField]
    public ProtoId<RCDPrototype> StartingProtoId { get; private set; }

    [DataField]
    public int Cost { get; private set; } = 1;

    [DataField("fx")]
    public NetEntity? Effect { get; private set; }

    private RCDDoAfterEvent() { }

    public RCDDoAfterEvent(NetCoordinates location, Direction direction, ProtoId<RCDPrototype> startingProtoId, int cost, NetEntity? effect = null)
    {
        Location = location;
        Direction = direction;
        StartingProtoId = startingProtoId;
        Cost = cost;
        Effect = effect;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
