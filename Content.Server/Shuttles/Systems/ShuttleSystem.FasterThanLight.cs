using Content.Server.Doors.Components;
using Content.Server.Doors.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Server.Stunnable;
using Content.Shared.Parallax;
using Content.Shared.Shuttles.Systems;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Shuttles.Events;
using Content.Shared.Buckle.Components;
using Content.Shared.Doors.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    /*
     * This is a way to move a shuttle from one location to another, via an intermediate map for fanciness.
     */

    [Dependency] private readonly AirlockSystem _airlock = default!;
    [Dependency] private readonly DoorSystem _doors = default!;
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StunSystem _stuns = default!;
    [Dependency] private readonly ThrusterSystem _thruster = default!;

    private MapId? _hyperSpaceMap;

    private const float DefaultStartupTime = 5.5f;
    private const float DefaultTravelTime = 30f;
    private const float DefaultArrivalTime = 5f;
    private const float FTLCooldown = 30f;

    private const float ShuttleFTLRange = 100f;

    /// <summary>
    /// Minimum mass a grid needs to be to block a shuttle recall.
    /// </summary>
    private const float ShuttleFTLMassThreshold = 300f;

    // I'm too lazy to make CVars.

    private readonly SoundSpecifier _startupSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_begin.ogg");
    // private SoundSpecifier _travelSound = new SoundPathSpecifier();
    private readonly SoundSpecifier _arrivalSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_end.ogg");

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

    private void InitializeFTL()
    {
        SubscribeLocalEvent<StationGridAddedEvent>(OnStationGridAdd);
        SubscribeLocalEvent<FTLDestinationComponent, EntityPausedEvent>(OnDestinationPause);
    }

    private void OnDestinationPause(EntityUid uid, FTLDestinationComponent component, ref EntityPausedEvent args)
    {
        _console.RefreshShuttleConsoles();
    }

    private void OnStationGridAdd(StationGridAddedEvent ev)
    {
        if (TryComp<PhysicsComponent>(ev.GridId, out var body) && body.Mass > 500f)
        {
            AddFTLDestination(ev.GridId, true);
        }
    }

    public bool CanFTL(EntityUid? uid, [NotNullWhen(false)] out string? reason, TransformComponent? xform = null)
    {
        reason = null;

        if (!TryComp<MapGridComponent>(uid, out var grid) ||
            !Resolve(uid.Value, ref xform))
        {
            return true;
        }

        var bounds = xform.WorldMatrix.TransformBox(grid.LocalAABB).Enlarged(ShuttleFTLRange);
        var bodyQuery = GetEntityQuery<PhysicsComponent>();

        foreach (var other in _mapManager.FindGridsIntersecting(xform.MapID, bounds))
        {
            if (grid.Owner == other.Owner ||
                !bodyQuery.TryGetComponent(other.Owner, out var body) ||
                body.Mass < ShuttleFTLMassThreshold) continue;

            reason = Loc.GetString("shuttle-console-proximity");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Adds a target for hyperspace to every shuttle console.
    /// </summary>
    public FTLDestinationComponent AddFTLDestination(EntityUid uid, bool enabled)
    {
        if (TryComp<FTLDestinationComponent>(uid, out var destination) && destination.Enabled == enabled) return destination;

        destination = EnsureComp<FTLDestinationComponent>(uid);

        if (HasComp<FTLComponent>(uid))
        {
            enabled = false;
        }

        destination.Enabled = enabled;
        _console.RefreshShuttleConsoles();
        return destination;
    }

    public void RemoveFTLDestination(EntityUid uid)
    {
        if (!RemComp<FTLDestinationComponent>(uid)) return;
        _console.RefreshShuttleConsoles();
    }

    /// <summary>
    /// Moves a shuttle from its current position to the target one. Goes through the hyperspace map while the timer is running.
    /// </summary>
    public void FTLTravel(ShuttleComponent component,
        EntityCoordinates coordinates,
        float startupTime = DefaultStartupTime,
        float hyperspaceTime = DefaultTravelTime)
    {
        if (!TrySetupFTL(component, out var hyperspace))
           return;

        hyperspace.StartupTime = startupTime;
        hyperspace.TravelTime = hyperspaceTime;
        hyperspace.Accumulator = hyperspace.StartupTime;
        hyperspace.TargetCoordinates = coordinates;
        hyperspace.Dock = false;
        _console.RefreshShuttleConsoles();
    }

    /// <summary>
    /// Moves a shuttle from its current position to docked on the target one. Goes through the hyperspace map while the timer is running.
    /// </summary>
    public void FTLTravel(ShuttleComponent component,
        EntityUid target,
        float startupTime = DefaultStartupTime,
        float hyperspaceTime = DefaultTravelTime,
        bool dock = false)
    {
        if (!TrySetupFTL(component, out var hyperspace))
            return;

        hyperspace.StartupTime = startupTime;
        hyperspace.TravelTime = hyperspaceTime;
        hyperspace.Accumulator = hyperspace.StartupTime;
        hyperspace.TargetUid = target;
        hyperspace.Dock = dock;
        _console.RefreshShuttleConsoles();
    }

    private bool TrySetupFTL(ShuttleComponent shuttle, [NotNullWhen(true)] out FTLComponent? component)
    {
        var uid = shuttle.Owner;
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
        foreach (var comp in EntityQuery<FTLComponent>())
        {
            comp.Accumulator -= frameTime;

            if (comp.Accumulator > 0f) continue;

            var xform = Transform(comp.Owner);
            PhysicsComponent? body;
            ShuttleComponent? shuttle;

            switch (comp.State)
            {
                // Startup time has elapsed and in hyperspace.
                case FTLState.Starting:
                    DoTheDinosaur(xform);

                    comp.State = FTLState.Travelling;

                    var width = Comp<MapGridComponent>(comp.Owner).LocalAABB.Width;
                    xform.Coordinates = new EntityCoordinates(_mapManager.GetMapEntityId(_hyperSpaceMap!.Value), new Vector2(_index + width / 2f, 0f));
                    xform.LocalRotation = Angle.Zero;
                    _index += width + Buffer;
                    comp.Accumulator += comp.TravelTime - DefaultArrivalTime;

                    if (TryComp(comp.Owner, out body))
                    {
                        _physics.SetLinearVelocity(comp.Owner, new Vector2(0f, 20f), body: body);
                        _physics.SetAngularVelocity(comp.Owner, 0f, body: body);
                        _physics.SetLinearDamping(body, 0f);
                        _physics.SetAngularDamping(body, 0f);
                    }

                    if (comp.TravelSound != null)
                    {
                        comp.TravelStream = SoundSystem.Play(comp.TravelSound.GetSound(),
                            Filter.Pvs(comp.Owner, 4f, entityManager: EntityManager), comp.TravelSound.Params);
                    }

                    SetDockBolts(comp.Owner, true);
                    _console.RefreshShuttleConsoles(comp.Owner);
                    break;
                // Arriving, play effects
                case FTLState.Travelling:
                    comp.Accumulator += DefaultArrivalTime;
                    comp.State = FTLState.Arriving;
                    // TODO: Arrival effects
                    // For now we'll just use the ss13 bubbles but we can do fancier.

                    if (TryComp(comp.Owner, out shuttle))
                    {
                        _thruster.DisableLinearThrusters(shuttle);
                        _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.South);
                    }

                    _console.RefreshShuttleConsoles(comp.Owner);
                    break;
                // Arrived
                case FTLState.Arriving:
                    DoTheDinosaur(xform);
                    SetDockBolts(comp.Owner, false);
                    SetDocks(comp.Owner, true);

                    if (TryComp(comp.Owner, out body))
                    {
                        _physics.SetLinearVelocity(comp.Owner, Vector2.Zero, body: body);
                        _physics.SetAngularVelocity(comp.Owner, 0f, body: body);
                        _physics.SetLinearDamping(body, ShuttleLinearDamping);
                        _physics.SetAngularDamping(body, ShuttleAngularDamping);
                    }

                    TryComp(comp.Owner, out shuttle);

                    if (comp.TargetUid != null && shuttle != null)
                    {
                        if (comp.Dock)
                            TryFTLDock(shuttle, comp.TargetUid.Value);
                        else
                            TryFTLProximity(shuttle, comp.TargetUid.Value);
                    }
                    else
                    {
                        xform.Coordinates = comp.TargetCoordinates;
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

                    SoundSystem.Play(_arrivalSound.GetSound(), Filter.Empty().AddInRange(Transform(comp.Owner).MapPosition, GetSoundRange(comp.Owner)), _arrivalSound.Params);

                    if (TryComp<FTLDestinationComponent>(comp.Owner, out var dest))
                    {
                        dest.Enabled = true;
                    }

                    comp.State = FTLState.Cooldown;
                    comp.Accumulator += FTLCooldown;
                    _console.RefreshShuttleConsoles(comp.Owner);
                    RaiseLocalEvent(new HyperspaceJumpCompletedEvent());
                    break;
                case FTLState.Cooldown:
                    RemComp<FTLComponent>(comp.Owner);
                    _console.RefreshShuttleConsoles(comp.Owner);
                    break;
                default:
                    _sawmill.Error($"Found invalid FTL state {comp.State} for {comp.Owner}");
                    RemComp<FTLComponent>(comp.Owner);
                    break;
            }
        }
    }

    private void SetDocks(EntityUid uid, bool enabled)
    {
        foreach (var (dock, xform) in EntityQuery<DockingComponent, TransformComponent>(true))
        {
            if (xform.ParentUid != uid || dock.Enabled == enabled) continue;
            _dockSystem.Undock(dock);
            dock.Enabled = enabled;
        }
    }

    private void SetDockBolts(EntityUid uid, bool enabled)
    {
        foreach (var (_, door, xform) in EntityQuery<DockingComponent, AirlockComponent, TransformComponent>(true))
        {
            if (xform.ParentUid != uid) continue;

            _doors.TryClose(door.Owner);
            _airlock.SetBoltsWithAudio(door.Owner, door, enabled);
        }
    }

    private float GetSoundRange(EntityUid uid)
    {
        if (!_mapManager.TryGetGrid(uid, out var grid)) return 4f;

        return MathF.Max(grid.LocalAABB.Width, grid.LocalAABB.Height) + 12.5f;
    }

    private void SetupHyperspace()
    {
        if (_hyperSpaceMap != null) return;

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
            if (!buckleQuery.TryGetComponent(child.Value, out var buckle) || buckle.Buckled) continue;

            toKnock.Add(child.Value);
        }
    }

    /// <summary>
    /// Tries to dock with the target grid, otherwise falls back to proximity.
    /// </summary>
    public bool TryFTLDock(ShuttleComponent component, EntityUid targetUid)
    {
        if (!TryComp<TransformComponent>(component.Owner, out var xform) ||
            !TryComp<TransformComponent>(targetUid, out var targetXform) ||
            targetXform.MapUid == null ||
            !targetXform.MapUid.Value.IsValid())
        {
            return false;
        }

        var config = GetDockingConfig(component, targetUid);

        if (config != null)
        {
           // Set position
           xform.Coordinates = config.Coordinates;
           xform.WorldRotation = config.Angle;

           // Connect everything
           foreach (var (dockA, dockB) in config.Docks)
           {
               _dockSystem.Dock(dockA, dockB);
           }

           return true;
        }

        TryFTLProximity(component, targetUid, xform, targetXform);
        return false;
    }

    /// <summary>
    /// Tries to arrive nearby without overlapping with other grids.
    /// </summary>
    public bool TryFTLProximity(ShuttleComponent component, EntityUid targetUid, TransformComponent? xform = null, TransformComponent? targetXform = null)
    {
        if (!Resolve(targetUid, ref targetXform) ||
            targetXform.MapUid == null ||
            !targetXform.MapUid.Value.IsValid() ||
            !Resolve(component.Owner, ref xform))
        {
            return false;
        }

        var xformQuery = GetEntityQuery<TransformComponent>();
        var shuttleAABB = Comp<MapGridComponent>(component.Owner).LocalAABB;
        Box2 targetLocalAABB;

        // Spawn nearby.
        // We essentially expand the Box2 of the target area until nothing else is added then we know it's valid.
        // Can't just get an AABB of every grid as we may spawn very far away.
        if (TryComp<MapGridComponent>(targetUid, out var targetGrid))
        {
            targetLocalAABB = targetGrid.LocalAABB;
        }
        else
        {
            targetLocalAABB = new Box2();
        }

        var targetAABB = _transform.GetWorldMatrix(targetXform, xformQuery)
            .TransformBox(targetLocalAABB).Enlarged(shuttleAABB.Size.Length);
        var nearbyGrids = new HashSet<EntityUid>(1) { targetUid };
        var iteration = 0;
        var lastCount = 1;
        var mapId = targetXform.MapID;

        while (iteration < FTLProximityIterations)
        {
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, targetAABB))
            {
                if (!nearbyGrids.Add(grid.Owner)) continue;

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
                if (nearbyGrids.Contains(grid.Owner)) continue;

                targetAABB = targetAABB.Union(_transform.GetWorldMatrix(grid.Owner, xformQuery)
                    .TransformBox(Comp<MapGridComponent>(grid.Owner).LocalAABB));
            }

            break;
        }

        var minRadius = (MathF.Max(targetAABB.Width, targetAABB.Height) + MathF.Max(shuttleAABB.Width, shuttleAABB.Height)) / 2f;
        var spawnPos = targetAABB.Center + _random.NextVector2(minRadius, minRadius + 64f);

        if (TryComp<PhysicsComponent>(component.Owner, out var shuttleBody))
        {
            _physics.SetLinearVelocity(component.Owner, Vector2.Zero, body: shuttleBody);
            _physics.SetAngularVelocity(component.Owner, 0f, body: shuttleBody);
        }

        xform.Coordinates = new EntityCoordinates(targetXform.MapUid.Value, spawnPos);
        xform.WorldRotation = _random.NextAngle();
        return true;
    }
}
