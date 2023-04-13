using Content.Server.Doors.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Shared.Parallax;
using Content.Shared.Shuttles.Systems;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Shuttles.Events;
using Content.Shared.Buckle.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    /*
     * This is a way to move a shuttle from one location to another, via an intermediate map for fanciness.
     */

    private MapId? _hyperSpaceMap;

    public const float DefaultStartupTime = 5.5f;
    public const float DefaultTravelTime = 30f;
    public const float DefaultArrivalTime = 5f;
    private const float FTLCooldown = 30f;
    private const float ShuttleFTLRange = 100f;

    /// <summary>
    /// Minimum mass a grid needs to be to block a shuttle recall.
    /// </summary>
    public const float ShuttleFTLMassThreshold = 300f;

    // I'm too lazy to make CVars.

    private readonly SoundSpecifier _startupSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_begin.ogg")
    {
        Params = AudioParams.Default.WithVolume(-5f),
    };
    // private SoundSpecifier _travelSound = new SoundPathSpecifier();
    private readonly SoundSpecifier _arrivalSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_end.ogg")
    {
        Params = AudioParams.Default.WithVolume(-5f),
    };

    private readonly TimeSpan _hyperspaceKnockdownTime = TimeSpan.FromSeconds(5);

    /// Left-side of the station we're allowed to use
    private float _index;

    /// <summary>
    /// Space between grids within hyperspace.
    /// </summary>
    private const float Buffer = 5f;

    /// <summary>
    /// How many times we try to proximity warp close to something before falling back to map-wideAABB.
    /// </summary>
    private const int FTLProximityIterations = 3;

    /// <summary>
    /// Minimum mass for an FTL destination
    /// </summary>
    public const float FTLDestinationMass = 500f;

    private void InitializeFTL()
    {
        SubscribeLocalEvent<StationGridAddedEvent>(OnStationGridAdd);
    }

    private void OnStationGridAdd(StationGridAddedEvent ev)
    {
        if (HasComp<MapComponent>(ev.GridId) ||
            TryComp<PhysicsComponent>(ev.GridId, out var body) &&
            body.Mass > FTLDestinationMass)
        {
            AddFTLDestination(ev.GridId, true);
        }
    }

    public bool CanFTL(EntityUid? uid, [NotNullWhen(false)] out string? reason)
    {
        if (HasComp<PreventPilotComponent>(uid))
        {
            reason = Loc.GetString("shuttle-console-prevent");
            return false;
        }

        reason = null;
        return true;
    }

    /// <summary>
    /// Adds a target for hyperspace to every shuttle console.
    /// </summary>
    public FTLDestinationComponent AddFTLDestination(EntityUid uid, bool enabled)
    {
        if (TryComp<FTLDestinationComponent>(uid, out var destination) && destination.Enabled == enabled)
            return destination;

        destination = EnsureComp<FTLDestinationComponent>(uid);

        if (HasComp<FTLComponent>(uid))
        {
            enabled = false;
        }

        destination.Enabled = enabled;
        _console.RefreshShuttleConsoles();
        return destination;
    }

    [PublicAPI]
    public void RemoveFTLDestination(EntityUid uid)
    {
        if (!RemComp<FTLDestinationComponent>(uid))
            return;

        _console.RefreshShuttleConsoles();
    }

    /// <summary>
    /// Moves a shuttle from its current position to the target one. Goes through the hyperspace map while the timer is running.
    /// </summary>
    public void FTLTravel(
        EntityUid shuttleUid,
        ShuttleComponent component,
        EntityCoordinates coordinates,
        float startupTime = DefaultStartupTime,
        float hyperspaceTime = DefaultTravelTime,
        string? priorityTag = null)
    {
        if (!TrySetupFTL(shuttleUid, component, out var hyperspace))
           return;

        hyperspace.StartupTime = startupTime;
        hyperspace.TravelTime = hyperspaceTime;
        hyperspace.Accumulator = hyperspace.StartupTime;
        hyperspace.TargetCoordinates = coordinates;
        hyperspace.Dock = false;
        hyperspace.PriorityTag = priorityTag;
        _console.RefreshShuttleConsoles();
    }

    /// <summary>
    /// Moves a shuttle from its current position to docked on the target one. Goes through the hyperspace map while the timer is running.
    /// </summary>
    public void FTLTravel(
        EntityUid shuttleUid,
        ShuttleComponent component,
        EntityUid target,
        float startupTime = DefaultStartupTime,
        float hyperspaceTime = DefaultTravelTime,
        bool dock = false,
        string? priorityTag = null)
    {
        if (!TrySetupFTL(shuttleUid, component, out var hyperspace))
            return;

        hyperspace.StartupTime = startupTime;
        hyperspace.TravelTime = hyperspaceTime;
        hyperspace.Accumulator = hyperspace.StartupTime;
        hyperspace.TargetUid = target;
        hyperspace.Dock = dock;
        hyperspace.PriorityTag = priorityTag;
        _console.RefreshShuttleConsoles();
    }

    private bool TrySetupFTL(EntityUid uid, ShuttleComponent shuttle, [NotNullWhen(true)] out FTLComponent? component)
    {
        component = null;

        if (HasComp<FTLComponent>(uid))
        {
            _sawmill.Warning($"Tried queuing {ToPrettyString(uid)} which already has HyperspaceComponent?");
            return false;
        }

        if (TryComp<FTLDestinationComponent>(uid, out var dest))
        {
            dest.Enabled = false;
        }

        _thruster.DisableLinearThrusters(shuttle);
        _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.North);
        _thruster.SetAngularThrust(shuttle, false);
        // TODO: Maybe move this to docking instead?
        SetDocks(uid, false);

        component = AddComp<FTLComponent>(uid);
        component.State = FTLState.Starting;
        // TODO: Need BroadcastGrid to not be bad.
        SoundSystem.Play(_startupSound.GetSound(), Filter.Empty().AddInRange(Transform(uid).MapPosition, GetSoundRange(component.Owner)), _startupSound.Params);
        // Make sure the map is setup before we leave to avoid pop-in (e.g. parallax).
        SetupHyperspace();
        return true;
    }

    private void UpdateHyperspace(float frameTime)
    {
        var query = EntityQueryEnumerator<FTLComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Accumulator -= frameTime;

            if (comp.Accumulator > 0f)
                continue;

            var xform = Transform(uid);
            PhysicsComponent? body;
            ShuttleComponent? shuttle;

            switch (comp.State)
            {
                // Startup time has elapsed and in hyperspace.
                case FTLState.Starting:
                    DoTheDinosaur(xform);

                    comp.State = FTLState.Travelling;
                    var fromMapUid = xform.MapUid;
                    var fromMatrix = _transform.GetWorldMatrix(xform);
                    var fromRotation = _transform.GetWorldRotation(xform);

                    var width = Comp<MapGridComponent>(uid).LocalAABB.Width;
                    xform.Coordinates = new EntityCoordinates(_mapManager.GetMapEntityId(_hyperSpaceMap!.Value), new Vector2(_index + width / 2f, 0f));
                    xform.LocalRotation = Angle.Zero;
                    _index += width + Buffer;
                    comp.Accumulator += comp.TravelTime - DefaultArrivalTime;

                    if (TryComp(uid, out body))
                    {
                        Enable(uid, body);
                        _physics.SetLinearVelocity(uid, new Vector2(0f, 20f), body: body);
                        _physics.SetAngularVelocity(uid, 0f, body: body);
                        _physics.SetLinearDamping(body, 0f);
                        _physics.SetAngularDamping(body, 0f);
                    }

                    SetDockBolts(uid, true);
                    _console.RefreshShuttleConsoles(uid);
                    var ev = new FTLStartedEvent(fromMapUid, fromMatrix, fromRotation);
                    RaiseLocalEvent(uid, ref ev);

                    if (comp.TravelSound != null)
                    {
                        comp.TravelStream = SoundSystem.Play(comp.TravelSound.GetSound(),
                            Filter.Pvs(uid, 4f, entityManager: EntityManager), comp.TravelSound.Params);
                    }
                    break;
                // Arriving, play effects
                case FTLState.Travelling:
                    comp.Accumulator += DefaultArrivalTime;
                    comp.State = FTLState.Arriving;
                    // TODO: Arrival effects
                    // For now we'll just use the ss13 bubbles but we can do fancier.

                    if (TryComp(uid, out shuttle))
                    {
                        _thruster.DisableLinearThrusters(shuttle);
                        _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.South);
                    }

                    _console.RefreshShuttleConsoles(uid);
                    break;
                // Arrived
                case FTLState.Arriving:
                    DoTheDinosaur(xform);
                    SetDockBolts(uid, false);
                    SetDocks(uid, true);

                    if (TryComp(uid, out body))
                    {
                        _physics.SetLinearVelocity(uid, Vector2.Zero, body: body);
                        _physics.SetAngularVelocity(uid, 0f, body: body);
                        _physics.SetLinearDamping(body, ShuttleLinearDamping);
                        _physics.SetAngularDamping(body, ShuttleAngularDamping);
                    }

                    TryComp(uid, out shuttle);
                    MapId mapId;

                    if (comp.TargetUid != null && shuttle != null)
                    {
                        if (comp.Dock)
                            TryFTLDock(uid, shuttle, comp.TargetUid.Value, comp.PriorityTag);
                        else
                            TryFTLProximity(uid, shuttle, comp.TargetUid.Value);

                        mapId = Transform(comp.TargetUid.Value).MapID;
                    }
                    else
                    {
                        xform.Coordinates = comp.TargetCoordinates;
                        mapId = comp.TargetCoordinates.GetMapId(EntityManager);
                    }

                    if (TryComp(uid, out body))
                    {
                        _physics.SetLinearVelocity(uid, Vector2.Zero, body: body);
                        _physics.SetAngularVelocity(uid, 0f, body: body);

                        // Disable shuttle if it's on a planet; unfortunately can't do this in parent change messages due
                        // to event ordering and awake body shenanigans (at least for now).
                        if (HasComp<MapGridComponent>(xform.MapUid))
                        {
                            Disable(uid, body);
                        }
                        else
                        {
                            Enable(uid, body);
                        }
                    }

                    if (shuttle != null)
                    {
                        _thruster.DisableLinearThrusters(shuttle);
                    }

                    if (comp.TravelStream != null)
                    {
                        comp.TravelStream?.Stop();
                        comp.TravelStream = null;
                    }

                    _audio.PlayGlobal(_arrivalSound, Filter.Empty().AddInRange(Transform(uid).MapPosition, GetSoundRange(uid)), true);

                    if (TryComp<FTLDestinationComponent>(uid, out var dest))
                    {
                        dest.Enabled = true;
                    }

                    comp.State = FTLState.Cooldown;
                    comp.Accumulator += FTLCooldown;
                    _console.RefreshShuttleConsoles(uid);
                    _mapManager.SetMapPaused(mapId, false);
                    var ftlEvent = new FTLCompletedEvent();
                    RaiseLocalEvent(uid, ref ftlEvent, true);
                    break;
                case FTLState.Cooldown:
                    RemComp<FTLComponent>(uid);
                    _console.RefreshShuttleConsoles(uid);
                    break;
                default:
                    _sawmill.Error($"Found invalid FTL state {comp.State} for {uid}");
                    RemComp<FTLComponent>(uid);
                    break;
            }
        }
    }

    private void SetDocks(EntityUid uid, bool enabled)
    {
        var query = AllEntityQuery<DockingComponent, TransformComponent>();

        while (query.MoveNext(out var dockUid, out var dock, out var xform))
        {
            if (xform.ParentUid != uid || dock.Enabled == enabled)
                continue;

            _dockSystem.Undock(dockUid, dock);
            dock.Enabled = enabled;
        }
    }

    private void SetDockBolts(EntityUid uid, bool enabled)
    {
        var query = AllEntityQuery<DockingComponent, AirlockComponent, TransformComponent>();

        while (query.MoveNext(out var doorUid, out _, out var door, out var xform))
        {
            if (xform.ParentUid != uid)
                continue;

            _doors.TryClose(doorUid);
            _airlock.SetBoltsWithAudio(doorUid, door, enabled);
        }
    }

    private float GetSoundRange(EntityUid uid)
    {
        if (!_mapManager.TryGetGrid(uid, out var grid))
            return 4f;

        return MathF.Max(grid.LocalAABB.Width, grid.LocalAABB.Height) + 12.5f;
    }

    private void SetupHyperspace()
    {
        if (_hyperSpaceMap != null)
            return;

        _hyperSpaceMap = _mapManager.CreateMap();
        _sawmill.Info($"Setup hyperspace map at {_hyperSpaceMap.Value}");
        DebugTools.Assert(!_mapManager.IsMapPaused(_hyperSpaceMap.Value));
        var parallax = EnsureComp<ParallaxComponent>(_mapManager.GetMapEntityId(_hyperSpaceMap.Value));
        parallax.Parallax = "FastSpace";
    }

    private void CleanupHyperspace()
    {
        _index = 0f;
        if (_hyperSpaceMap == null || !_mapManager.MapExists(_hyperSpaceMap.Value))
        {
            _hyperSpaceMap = null;
            return;
        }
        _mapManager.DeleteMap(_hyperSpaceMap.Value);
        _hyperSpaceMap = null;
    }

    /// <summary>
    /// Puts everyone unbuckled on the floor, paralyzed.
    /// </summary>
    private void DoTheDinosaur(TransformComponent xform)
    {
        var buckleQuery = GetEntityQuery<BuckleComponent>();
        var statusQuery = GetEntityQuery<StatusEffectsComponent>();
        // Get enumeration exceptions from people dropping things if we just paralyze as we go
        var toKnock = new ValueList<EntityUid>();

        KnockOverKids(xform, buckleQuery, statusQuery, ref toKnock);

        foreach (var child in toKnock)
        {
            if (!statusQuery.TryGetComponent(child, out var status)) continue;
            _stuns.TryParalyze(child, _hyperspaceKnockdownTime, true, status);
        }
    }

    private void KnockOverKids(TransformComponent xform, EntityQuery<BuckleComponent> buckleQuery, EntityQuery<StatusEffectsComponent> statusQuery, ref ValueList<EntityUid> toKnock)
    {
        // Not recursive because probably not necessary? If we need it to be that's why this method is separate.
        var childEnumerator = xform.ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            if (!buckleQuery.TryGetComponent(child.Value, out var buckle) || buckle.Buckled)
                continue;

            toKnock.Add(child.Value);
        }
    }

    /// <summary>
    /// Tries to dock with the target grid, otherwise falls back to proximity.
    /// </summary>
    public bool TryFTLDock(EntityUid shuttleUid, ShuttleComponent component, EntityUid targetUid, string? priorityTag = null)
    {
        if (!TryComp<TransformComponent>(shuttleUid, out var shuttleXform) ||
            !TryComp<TransformComponent>(targetUid, out var targetXform) ||
            targetXform.MapUid == null ||
            !targetXform.MapUid.Value.IsValid())
        {
            return false;
        }

        var config = _dockSystem.GetDockingConfig(shuttleUid, targetUid, priorityTag);

        if (config != null)
        {
            FTLDock(config, shuttleXform);
            return true;
        }

        TryFTLProximity(shuttleUid, component, targetUid, shuttleXform, targetXform);
        return false;
    }

    /// <summary>
    /// Forces an FTL dock.
    /// </summary>
    public void FTLDock(DockingConfig config, TransformComponent shuttleXform)
    {
        // Set position
        shuttleXform.Coordinates = config.Coordinates;
        _transform.SetWorldRotation(shuttleXform, config.Angle);

        // Connect everything
        foreach (var (dockAUid, dockBUid, dockA, dockB) in config.Docks)
        {
            _dockSystem.Dock(dockAUid, dockA, dockBUid, dockB);
        }
    }

    /// <summary>
    /// Tries to arrive nearby without overlapping with other grids.
    /// </summary>
    public bool TryFTLProximity(EntityUid shuttleUid, ShuttleComponent component, EntityUid targetUid, TransformComponent? xform = null, TransformComponent? targetXform = null)
    {
        if (!Resolve(targetUid, ref targetXform) ||
            targetXform.MapUid == null ||
            !targetXform.MapUid.Value.IsValid() ||
            !Resolve(shuttleUid, ref xform))
        {
            return false;
        }

        var xformQuery = GetEntityQuery<TransformComponent>();
        var shuttleAABB = Comp<MapGridComponent>(shuttleUid).LocalAABB;
        Box2 targetLocalAABB;

        // Spawn nearby.
        // We essentially expand the Box2 of the target area until nothing else is added then we know it's valid.
        // Can't just get an AABB of every grid as we may spawn very far away.
        if (TryComp<MapGridComponent>(targetXform.GridUid, out var targetGrid))
        {
            targetLocalAABB = targetGrid.LocalAABB;
        }
        else
        {
            targetLocalAABB = new Box2();
        }

        var targetAABB = _transform.GetWorldMatrix(targetXform, xformQuery)
            .TransformBox(targetLocalAABB).Enlarged(shuttleAABB.Size.Length);
        var nearbyGrids = new HashSet<EntityUid>();
        var iteration = 0;
        var lastCount = nearbyGrids.Count;
        var mapId = targetXform.MapID;

        while (iteration < FTLProximityIterations)
        {
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, targetAABB))
            {
                if (!nearbyGrids.Add(grid.Owner))
                    continue;

                targetAABB = targetAABB.Union(_transform.GetWorldMatrix(grid.Owner, xformQuery)
                    .TransformBox(Comp<MapGridComponent>(grid.Owner).LocalAABB));
            }

            // Can do proximity
            if (nearbyGrids.Count == lastCount)
            {
                break;
            }

            targetAABB = targetAABB.Enlarged(shuttleAABB.Size.Length / 2f);
            iteration++;
            lastCount = nearbyGrids.Count;

            // Mishap moment, dense asteroid field or whatever
            if (iteration != FTLProximityIterations)
                continue;

            foreach (var grid in _mapManager.GetAllGrids())
            {
                // Don't add anymore as it is irrelevant, but that doesn't mean we need to re-do existing work.
                if (nearbyGrids.Contains(grid.Owner))
                    continue;

                targetAABB = targetAABB.Union(_transform.GetWorldMatrix(grid.Owner, xformQuery)
                    .TransformBox(Comp<MapGridComponent>(grid.Owner).LocalAABB));
            }

            break;
        }

        Vector2 spawnPos;

        if (TryComp<PhysicsComponent>(shuttleUid, out var shuttleBody))
        {
            _physics.SetLinearVelocity(shuttleUid, Vector2.Zero, body: shuttleBody);
            _physics.SetAngularVelocity(shuttleUid, 0f, body: shuttleBody);
        }

        // TODO: This is pretty crude for multiple landings.
        if (nearbyGrids.Count > 1 || !HasComp<MapComponent>(targetXform.GridUid))
        {
            var minRadius = (MathF.Max(targetAABB.Width, targetAABB.Height) + MathF.Max(shuttleAABB.Width, shuttleAABB.Height)) / 2f;
            spawnPos = targetAABB.Center + _random.NextVector2(minRadius, minRadius + 64f);
        }
        else if (shuttleBody != null)
        {
            var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetXform, xformQuery);
            var transform = new Transform(targetPos, targetRot);
            spawnPos = Robust.Shared.Physics.Transform.Mul(transform, -shuttleBody.LocalCenter);
        }
        else
        {
            spawnPos = _transform.GetWorldPosition(targetXform, xformQuery);
        }

        xform.Coordinates = new EntityCoordinates(targetXform.MapUid.Value, spawnPos);

        if (!HasComp<MapComponent>(targetXform.GridUid))
        {
            _transform.SetLocalRotation(xform, _random.NextAngle());
        }
        else
        {
            _transform.SetLocalRotation(xform, Angle.Zero);
        }

        return true;
    }
}
