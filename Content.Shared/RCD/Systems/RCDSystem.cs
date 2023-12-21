using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Construction;
using Content.Shared.Construction.Components;
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
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace Content.Shared.RCD.Systems;

public sealed class RCDSystem : EntitySystem
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
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RCDComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RCDComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RCDComponent, RCDDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RCDComponent, DoAfterAttemptEvent<RCDDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<RCDComponent, RCDSystemMessage>(OnRCDSystemMessage);
    }

    #region Event handling

    private void OnRCDSystemMessage(EntityUid uid, RCDComponent component, RCDSystemMessage args)
    {
        component.Mode = args.RcdMode;
        component.ConstructionPrototype = args.ConstructionPrototype;

        Dirty(uid, component);
    }

    private void OnExamine(EntityUid uid, RCDComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var msg = Loc.GetString("rcd-component-examine-detail", ("mode", comp.Mode));
        args.PushMarkup(msg);
    }

    private void OnAfterInteract(EntityUid uid, RCDComponent component, AfterInteractEvent args)
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

        var gridUid = location.GetGridUid(EntityManager);

        if (!TryGetMapGrid(gridUid, location, out var mapGrid))
            return;

        gridUid = mapGrid.Owner;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.Delay,
            new RCDDoAfterEvent(GetNetCoordinates(location), component.Mode, component.ConstructionPrototype),
            uid, target: args.Target, used: uid)
        {
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnUserMove = true,
            BreakOnTargetMove = args.Target != null,
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        args.Handled = true;

        if (_doAfter.TryStartDoAfter(doAfterArgs) &&
            _gameTiming.IsFirstTimePredicted &&
            _net.IsServer &&
            TryToFinalizeConstruction(uid, component, component.Mode, component.ConstructionPrototype, location, args.Target, args.User, true))
        {
            var effect = Spawn("EffectRCDConstruction", location);
            //Transform(effect).LocalRotation = Transform(gridUid.Value).LocalRotation.GetCardinalDir().ToAngle();
        }
    }

    private void OnDoAfterAttempt(EntityUid uid, RCDComponent component, DoAfterAttemptEvent<RCDDoAfterEvent> args)
    {
        if (args.Event?.DoAfter?.Args == null)
            return;

        var location = GetCoordinates(args.Event.Location);

        if (!TryToFinalizeConstruction(uid, component, args.Event.StartingMode, args.Event.StartingPrototype, location, args.Event.Target, args.Event.User, true))
        {
            args.Cancel();
            return;
        }
    }

    private void OnDoAfter(EntityUid uid, RCDComponent component, RCDDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !_timing.IsFirstTimePredicted)
            return;

        args.Handled = true;

        var location = GetCoordinates(args.Location);

        // Try to construct the prototype
        if (!TryToFinalizeConstruction(uid, component, component.Mode, component.ConstructionPrototype, location, args.Target, args.User, false))
            return;

        // Play audio and consume charge if successful
        _audio.PlayPredicted(component.SuccessSound, uid, args.User);
        _charges.UseCharge(uid);
    }

    #endregion

    private bool TryToFinalizeConstruction
        (EntityUid uid,
        RCDComponent component,
        RcdMode rcdMode,
        string? constructionPrototype,
        EntityCoordinates location,
        EntityUid? target,
        EntityUid user,
        bool dryRun = true)
    {
        // Exit if the construction mode has changed
        if (component.Mode != rcdMode)
            return false;

        // Exit if the construction prototype has changed
        if (component.ConstructionPrototype != constructionPrototype)
            return false;

        // Gather location data
        var gridUid = location.GetGridUid(EntityManager);

        if (!TryGetMapGrid(gridUid, location, out var mapGrid))
            return false;

        gridUid = mapGrid.Owner;

        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);
        var mapGridData = new MapGridData(gridUid.Value, mapGrid, location, tile, position);

        // Exit if the target / target location is obstructed
        var unobstructed = target == null
            ? _interaction.InRangeUnobstructed(user, _mapSystem.GridTileToWorld(gridUid.Value, mapGrid, tile.GridIndices), popup: true)
            : _interaction.InRangeUnobstructed(user, target.Value, popup: true);

        if (!unobstructed)
            return false;

        // Try to construct the prototype (or if this is a dry run, check that the construct is still valid instead)
        switch (component.Mode)
        {
            case RcdMode.Deconstruct: return TryToDeconstruct(uid, component, mapGridData, target, user, dryRun);
            case RcdMode.Floors: return TryToConstructFloor(uid, component, mapGridData, user, dryRun);
            case RcdMode.Catwalks: return TryToConstructCatwalk(uid, component, mapGridData, user, dryRun);
            case RcdMode.Walls: return TryToConstructWall(uid, component, mapGridData, user, dryRun);
            case RcdMode.Airlocks: return TryToConstructAirlock(uid, component, mapGridData, user, dryRun);
            case RcdMode.Windows: return TryToConstructWindow(uid, component, mapGridData, user, false, dryRun);
            case RcdMode.DirectionalWindows: return TryToConstructWindow(uid, component, mapGridData, user, true, dryRun);
            case RcdMode.Machines: return TryToConstructMachine(uid, component, mapGridData, user, false, dryRun);
            case RcdMode.Computers: return TryToConstructMachine(uid, component, mapGridData, user, true, dryRun);
            case RcdMode.Lighting: return TryToConstructLighting(uid, component, mapGridData, user, dryRun);
        }

        return false;
    }

    #region Entity construction/deconstruction checks and entity spawning/deletion

    private bool TryToDeconstruct(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid? target, EntityUid user, bool dryRun = true)
    {
        if (mapGridData.Tile.Tile.IsEmpty)
            return false;

        // Attempt to deconstruct a floor tile
        if (target == null)
        {
            // The tile has a structure sitting on it
            if (IsStructurePlaceable(mapGridData, uid, user))
            {
                _popup.PopupClient(Loc.GetString("rcd-component-tile-obstructed-message"), uid, user);
                return false;
            }

            // The floor tile cannot be destroyed
            var tileDef = (ContentTileDefinition) _tileDefMan[mapGridData.Tile.Tile.TypeId];
            if (tileDef.Indestructible)
            {
                _popup.PopupClient(Loc.GetString("rcd-component-tile-indestructible-message"), uid, user);
                return false;
            }
        }

        // Attempt to deconstruct an object They tried to decon a non-turf but it's not in the whitelist
        else
        {
            if (!_tag.HasTag(target.Value, "RCDDeconstructWhitelist"))
            {
                _popup.PopupClient(Loc.GetString("rcd-component-deconstruct-target-not-on-whitelist-message"), uid, user);
                return false;
            }
        }

        if (dryRun || !_net.IsServer)
            return true;

        // Deconstruct the tile
        if (!IsStructurePlaceable(mapGridData, uid, user))
        {
            _mapSystem.SetTile(mapGridData.GridUid, mapGridData.Component, mapGridData.Position, Tile.Empty);
            _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to set grid: {mapGridData.GridUid} tile: {mapGridData.Position} to space");
        }

        // Deconstruct the object
        else if (target != null)
        {
            _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to delete {ToPrettyString(target):target}");
            QueueDel(target);
        }

        return true;
    }

    private bool TryToConstructFloor(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid user, bool dryRun = true)
    {
        if (!IsFloorPlaceable(component.ConstructionPrototype, mapGridData, uid, user))
            return false;

        if (dryRun || !_net.IsServer)
            return true;

        _mapSystem.SetTile(mapGridData.GridUid, mapGridData.Component, mapGridData.Position, new Tile(_tileDefMan[component.ConstructionPrototype!].TileId));

        _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to set grid: {mapGridData.GridUid} {mapGridData.Position} to {component.ConstructionPrototype}");
        return true;
    }

    private bool TryToConstructCatwalk(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid user, bool dryRun = true)
    {
        if (!IsCatwalkPlaceable(component.ConstructionPrototype, mapGridData, uid, user))
            return false;

        if (dryRun || !_net.IsServer)
            return true;

        var ent = Spawn(component.ConstructionPrototype, _mapSystem.GridTileToLocal(mapGridData.GridUid, mapGridData.Component, mapGridData.Position));
        Transform(ent).LocalRotation = Angle.Zero;

        _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to set grid: {mapGridData.GridUid} {mapGridData.Position} to {component.ConstructionPrototype}");
        return true;
    }

    private bool TryToConstructWall(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid user, bool dryRun = true)
    {
        if (!IsStructurePlaceable(mapGridData, uid, user))
            return false;

        if (dryRun || !_net.IsServer)
            return true;

        var ent = Spawn(component.ConstructionPrototype, _mapSystem.GridTileToLocal(mapGridData.GridUid, mapGridData.Component, mapGridData.Position));
        Transform(ent).LocalRotation = Angle.Zero;

        _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to spawn {ToPrettyString(ent)} at {mapGridData.Position} on grid {mapGridData.GridUid}");
        return true;
    }

    private bool TryToConstructAirlock(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid user, bool dryRun = true)
    {
        if (!IsStructurePlaceable(mapGridData, uid, user))
            return false;

        if (dryRun || !_net.IsServer)
            return true;

        var ent = Spawn(component.ConstructionPrototype, _mapSystem.GridTileToLocal(mapGridData.GridUid, mapGridData.Component, mapGridData.Position));
        Transform(ent).LocalRotation = Transform(uid).LocalRotation;

        _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to spawn {ToPrettyString(ent)} at {mapGridData.Position} on grid {mapGridData.GridUid}");
        return true;
    }

    private bool TryToConstructWindow(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid user, bool isDirectional = false, bool dryRun = true)
    {
        if (!IsWindowPlaceable(isDirectional, mapGridData, uid, user))
            return false;

        if (dryRun || !_net.IsServer)
            return true;

        var ent = Spawn(component.ConstructionPrototype, _mapSystem.GridTileToLocal(mapGridData.GridUid, mapGridData.Component, mapGridData.Position));
        Transform(ent).LocalRotation = GetAngleFromUserFacing(user);

        _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to spawn {ToPrettyString(ent)} at {mapGridData.Position} on grid {mapGridData.GridUid}");
        return true;
    }

    private bool TryToConstructMachine(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid user, bool isComputer, bool dryRun = true)
    {
        if (!IsStructurePlaceable(mapGridData, uid, user))
            return false;

        if (dryRun || !_net.IsServer)
            return true;

        string? prototype = component.ConstructionPrototype;

        foreach (var heldObject in _hands.EnumerateHeld(user))
        {
            if (isComputer && TryComp<ComputerBoardComponent>(heldObject, out var computerBoard))
            {
                prototype = computerBoard.Prototype;
                QueueDel(heldObject);
                break;
            }

            else if (!isComputer && TryComp<MachineBoardComponent>(heldObject, out var machineBoard))
            {
                prototype = machineBoard.Prototype;
                QueueDel(heldObject);
                break;
            }
        }

        var ent = Spawn(prototype, _mapSystem.GridTileToLocal(mapGridData.GridUid, mapGridData.Component, mapGridData.Position));
        Transform(ent).LocalRotation = GetAngleOppositeOfUserFacing(user);

        _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to spawn {ToPrettyString(ent)} at {mapGridData.Position} on grid {mapGridData.GridUid}");
        return true;
    }

    private bool TryToConstructLighting(EntityUid uid, RCDComponent component, MapGridData mapGridData, EntityUid user, bool dryRun = true)
    {
        if (!IsWallAttachmentPlaceable(mapGridData, uid, user))
            return false;

        if (dryRun || !_net.IsServer)
            return true;

        var ent = Spawn(component.ConstructionPrototype, _mapSystem.GridTileToLocal(mapGridData.GridUid, mapGridData.Component, mapGridData.Position));
        Transform(ent).LocalRotation = GetAngleOppositeOfUserFacing(user);

        _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RCD to spawn {ToPrettyString(ent)} at {mapGridData.Position} on grid {mapGridData.GridUid}");
        return true;
    }

    #endregion

    #region Entity placement checks

    private bool IsFloorPlaceable(string? floorPrototype, MapGridData mapGridData, EntityUid uid, EntityUid user)
    {
        if (floorPrototype == null)
            return false;

        if (!mapGridData.Tile.Tile.IsEmpty &&
            mapGridData.Tile.Tile.GetContentTileDefinition().ID == floorPrototype)
        {
            _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-as-tile-not-empty-message"), uid, user);
            return false;
        }

        if (!_floors.CanPlaceTile(mapGridData.GridUid, mapGridData.Component, out var reason))
        {
            _popup.PopupClient(reason, user, user);
            return false;
        }

        return true;
    }

    private bool IsCatwalkPlaceable(string? catwalkPrototype, MapGridData mapGridData, EntityUid uid, EntityUid user)
    {
        if (catwalkPrototype == null)
            return false;

        if (mapGridData.Tile.Tile.IsEmpty)
        {
            _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-on-empty-space-message"), uid, user);
            return false;
        }

        if (!mapGridData.Tile.Tile.GetContentTileDefinition().IsSubFloor)
        {
            _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-as-tile-requires-subfloor-message"), uid, user);
            return false;
        }

        if (_turf.IsTileBlocked(mapGridData.Tile, CollisionGroup.SmallMobLayer))
        {
            _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-as-tile-not-empty-message"), uid, user);
            return false;
        }

        return true;
    }

    private bool IsStructurePlaceable(MapGridData mapGridData, EntityUid uid, EntityUid user)
    {
        if (mapGridData.Tile.Tile.IsEmpty)
        {
            _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-on-empty-space-message"), uid, user);
            return false;
        }

        if (_turf.IsTileBlocked(mapGridData.Tile, CollisionGroup.MobMask))
        {
            _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-as-tile-not-empty-message"), uid, user);
            return false;
        }

        return true;
    }

    private bool IsWallAttachmentPlaceable(MapGridData mapGridData, EntityUid uid, EntityUid user)
    {
        if (mapGridData.Tile.Tile.IsEmpty)
        {
            _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-on-empty-space-message"), uid, user);
            return false;
        }

        if (_turf.IsTileBlocked(mapGridData.Tile, CollisionGroup.FlyingMobMask))
        {
            _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-as-tile-not-empty-message"), uid, user);
            return false;
        }

        return true;
    }

    private bool IsWindowPlaceable(bool isDirectional, MapGridData mapGridData, EntityUid uid, EntityUid user)
    {
        if (mapGridData.Tile.Tile.IsEmpty)
        {
            _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-on-empty-space-message"), uid, user);
            return false;
        }

        foreach (var ent in _lookup.GetEntitiesIntersecting(mapGridData.Tile, -0.1f, LookupFlags.Approximate | LookupFlags.Static))
        {
            // Windows can be built on this entity; continue
            if (HasComp<SharedCanBuildWindowOnTopComponent>(ent))
                continue;

            // This entity has no fixtures; continue
            if (!TryComp<FixturesComponent>(ent, out var fixtures))
                continue;

            for (int i = 0; i < fixtures.FixtureCount; i++)
            {
                (var _, var fixture) = fixtures.Fixtures.ElementAt(i);

                // This fixture does not collison with the window; continue
                if ((fixture.CollisionLayer & (int) CollisionGroup.MobMask) == 0)
                    continue;

                // Directional windows have slim profiles and may not intersect with the fixture
                if (isDirectional)
                {
                    // Make a small collision box for the directional window being built
                    var (entWorldPos, entWorldRot) = _transform.GetWorldPositionRotation(Transform(ent));
                    var entXform = new Transform(entWorldPos, entWorldRot);

                    var box2 = new Box2(-0.23f, -0.49f, 0.23f, -0.36f);
                    var verts = new ValueList<Vector2>()
                    {
                        box2.BottomLeft,
                        box2.BottomRight,
                        box2.TopRight,
                        box2.TopLeft
                    };

                    var poly = new PolygonShape();
                    poly.Set(verts.Span, 4);

                    var polyXform = new Transform(entWorldPos, (float) GetAngleFromUserFacing(user));

                    // The directional window being built will not collide with the fixture after all; continue
                    if (!poly.ComputeAABB(polyXform, 0).Intersects(fixture.Shape.ComputeAABB(entXform, 0)))
                        continue;
                }

                // Collision detected
                _popup.PopupClient(Loc.GetString("rcd-component-cannot-build-as-tile-not-empty-message"), uid, user);
                return false;
            }
        }

        return true;
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

    private Angle GetAngleFromUserFacing(EntityUid user)
    {
        return Transform(user).LocalRotation.GetCardinalDir().ToAngle();
    }

    private Angle GetAngleOppositeOfUserFacing(EntityUid user)
    {
        return Transform(user).LocalRotation.Opposite().GetCardinalDir().ToAngle();
    }

    #endregion
}

[Serializable, NetSerializable]
public struct RCDData
{
    public RcdMode RCDMode;
    public string? ConstructionPrototype;

    public RCDData()
    {

    }
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
    public NetCoordinates Location = default!;

    [DataField("startingMode", required: true)]
    public RcdMode StartingMode = default!;

    [DataField("startingPrototype")]
    public string? StartingPrototype = null;

    private RCDDoAfterEvent() { }

    public RCDDoAfterEvent(NetCoordinates location, RcdMode startingMode, string? startingPrototype = null)
    {
        Location = location;
        StartingMode = startingMode;
        StartingPrototype = startingPrototype;
    }

    public override DoAfterEvent Clone() => this;
}
