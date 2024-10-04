using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Events;
using Content.Shared.Body.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Maps;
using Content.Shared.Parallax;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Timing;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using FTLMapComponent = Content.Shared.Shuttles.Components.FTLMapComponent;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    /*
     * This is a way to move a shuttle from one location to another, via an intermediate map for fanciness.
     */

    private readonly SoundSpecifier _startupSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_begin.ogg")
    {
        Params = AudioParams.Default.WithVolume(-5f),
    };

    private readonly SoundSpecifier _arrivalSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_end.ogg")
    {
        Params = AudioParams.Default.WithVolume(-5f),
    };

    public float DefaultStartupTime;
    public float DefaultTravelTime;
    public float DefaultArrivalTime;
    private float FTLCooldown;
    public float FTLMassLimit;
    private TimeSpan _hyperspaceKnockdownTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Left-side of the station we're allowed to use
    /// </summary>
    private float _index;

    /// <summary>
    /// Space between grids within hyperspace.
    /// </summary>
    private const float Buffer = 5f;

    /// <summary>
    /// How many times we try to proximity warp close to something before falling back to map-wideAABB.
    /// </summary>
    private const int FTLProximityIterations = 5;

    private readonly HashSet<EntityUid> _lookupEnts = new();
    private readonly HashSet<EntityUid> _immuneEnts = new();
    private readonly HashSet<Entity<NoFTLComponent>> _noFtls = new();

    private EntityQuery<BodyComponent> _bodyQuery;
    private EntityQuery<BuckleComponent> _buckleQuery;
    private EntityQuery<FTLSmashImmuneComponent> _immuneQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<StatusEffectsComponent> _statusQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private void InitializeFTL()
    {
        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);
        SubscribeLocalEvent<FTLComponent, ComponentShutdown>(OnFtlShutdown);

        _bodyQuery = GetEntityQuery<BodyComponent>();
        _buckleQuery = GetEntityQuery<BuckleComponent>();
        _immuneQuery = GetEntityQuery<FTLSmashImmuneComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _statusQuery = GetEntityQuery<StatusEffectsComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        _cfg.OnValueChanged(CCVars.FTLStartupTime, time => DefaultStartupTime = time, true);
        _cfg.OnValueChanged(CCVars.FTLTravelTime, time => DefaultTravelTime = time, true);
        _cfg.OnValueChanged(CCVars.FTLArrivalTime, time => DefaultArrivalTime = time, true);
        _cfg.OnValueChanged(CCVars.FTLCooldown, time => FTLCooldown = time, true);
        _cfg.OnValueChanged(CCVars.FTLMassLimit, time => FTLMassLimit = time, true);
        _cfg.OnValueChanged(CCVars.HyperspaceKnockdownTime, time => _hyperspaceKnockdownTime = TimeSpan.FromSeconds(time), true);
    }

    private void OnFtlShutdown(Entity<FTLComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.VisualizerEntity);
        ent.Comp.VisualizerEntity = null;
    }

    private void OnStationPostInit(ref StationPostInitEvent ev)
    {
        // Add all grid maps as ftl destinations that anyone can FTL to.
        foreach (var gridUid in ev.Station.Comp.Grids)
        {
            var gridXform = _xformQuery.GetComponent(gridUid);

            if (gridXform.MapUid == null)
            {
                continue;
            }

            TryAddFTLDestination(gridXform.MapID, true, false, false, out _);
        }
    }

    /// <summary>
    /// Ensures the FTL map exists and returns it.
    /// </summary>
    private EntityUid EnsureFTLMap()
    {
        var query = AllEntityQuery<FTLMapComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            return uid;
        }

        var mapUid = _mapSystem.CreateMap(out var mapId);
        var ftlMap = AddComp<FTLMapComponent>(mapUid);

        _metadata.SetEntityName(mapUid, "FTL");
        Log.Debug($"Setup hyperspace map at {mapUid}");
        DebugTools.Assert(!_mapSystem.IsPaused(mapId));
        var parallax = EnsureComp<ParallaxComponent>(mapUid);
        parallax.Parallax = ftlMap.Parallax;

        return mapUid;
    }

    public StartEndTime GetStateTime(FTLComponent component)
    {
        var state = component.State;

        switch (state)
        {
            case FTLState.Starting:
            case FTLState.Travelling:
            case FTLState.Arriving:
            case FTLState.Cooldown:
                return component.StateTime;
            case FTLState.Available:
                return default;
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Updates the whitelist for this FTL destination.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="whitelist"></param>
    public void SetFTLWhitelist(Entity<FTLDestinationComponent?> entity, EntityWhitelist? whitelist)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (entity.Comp.Whitelist == whitelist)
            return;

        entity.Comp.Whitelist = whitelist;
        _console.RefreshShuttleConsoles();
        Dirty(entity);
    }

    /// <summary>
    /// Adds the target map as available for FTL.
    /// </summary>
    public bool TryAddFTLDestination(MapId mapId, bool enabled, [NotNullWhen(true)] out FTLDestinationComponent? component)
    {
        return TryAddFTLDestination(mapId, enabled, true, false, out component);
    }

    public bool TryAddFTLDestination(MapId mapId, bool enabled, bool requireDisk, bool beaconsOnly, [NotNullWhen(true)] out FTLDestinationComponent? component)
    {
        var mapUid = _mapSystem.GetMapOrInvalid(mapId);
        component = null;

        if (!Exists(mapUid))
            return false;

        component = EnsureComp<FTLDestinationComponent>(mapUid);

        if (component.Enabled == enabled && component.RequireCoordinateDisk == requireDisk && component.BeaconsOnly == beaconsOnly)
            return true;

        component.Enabled = enabled;
        component.RequireCoordinateDisk = requireDisk;
        component.BeaconsOnly = beaconsOnly;

        _console.RefreshShuttleConsoles();
        Dirty(mapUid, component);
        return true;
    }

    [PublicAPI]
    public void RemoveFTLDestination(EntityUid uid)
    {
        if (!RemComp<FTLDestinationComponent>(uid))
            return;

        _console.RefreshShuttleConsoles();
    }

    /// <summary>
    /// Returns true if the grid can FTL. Used to block protected shuttles like the emergency shuttle.
    /// </summary>
    public bool CanFTL(EntityUid shuttleUid, [NotNullWhen(false)] out string? reason)
    {
        // Currently in FTL already
        if (HasComp<FTLComponent>(shuttleUid))
        {
            reason = Loc.GetString("shuttle-console-in-ftl");
            return false;
        }

        if (TryComp<PhysicsComponent>(shuttleUid, out var shuttlePhysics))
        {
            // Static physics type is set when station anchor is enabled
            if (shuttlePhysics.BodyType == BodyType.Static)
            {
                reason = Loc.GetString("shuttle-console-static");
                return false;
            }

            // Too large to FTL
            if (FTLMassLimit > 0 &&  shuttlePhysics.Mass > FTLMassLimit)
            {
                reason = Loc.GetString("shuttle-console-mass");
                return false;
            }
        }

        if (HasComp<PreventPilotComponent>(shuttleUid))
        {
            reason = Loc.GetString("shuttle-console-prevent");
            return false;
        }

        var ev = new ConsoleFTLAttemptEvent(shuttleUid, false, string.Empty);
        RaiseLocalEvent(shuttleUid, ref ev, true);

        if (ev.Cancelled)
        {
            reason = ev.Reason;
            return false;
        }

        reason = null;
        return true;
    }

    /// <summary>
    /// Moves a shuttle from its current position to the target one without any checks. Goes through the hyperspace map while the timer is running.
    /// </summary>
    public void FTLToCoordinates(
        EntityUid shuttleUid,
        ShuttleComponent component,
        EntityCoordinates coordinates,
        Angle angle,
        float? startupTime = null,
        float? hyperspaceTime = null,
        string? priorityTag = null)
    {
        if (!TrySetupFTL(shuttleUid, component, out var hyperspace))
            return;

        startupTime ??= DefaultStartupTime;
        hyperspaceTime ??= DefaultTravelTime;

        hyperspace.StartupTime = startupTime.Value;
        hyperspace.TravelTime = hyperspaceTime.Value;
        hyperspace.StateTime = StartEndTime.FromStartDuration(
            _gameTiming.CurTime,
            TimeSpan.FromSeconds(hyperspace.StartupTime));
        hyperspace.TargetCoordinates = coordinates;
        hyperspace.TargetAngle = angle;
        hyperspace.PriorityTag = priorityTag;

        _console.RefreshShuttleConsoles(shuttleUid);

        var mapId = _transform.GetMapId(coordinates);
        var mapUid = _mapSystem.GetMap(mapId);
        var ev = new FTLRequestEvent(mapUid);
        RaiseLocalEvent(shuttleUid, ref ev, true);
    }

    /// <summary>
    /// Moves a shuttle from its current position to docked on the target one.
    /// If no docks are free when FTLing it will arrive in proximity
    /// </summary>
    public void FTLToDock(
        EntityUid shuttleUid,
        ShuttleComponent component,
        EntityUid target,
        float? startupTime = null,
        float? hyperspaceTime = null,
        string? priorityTag = null)
    {
        if (!TrySetupFTL(shuttleUid, component, out var hyperspace))
            return;

        startupTime ??= DefaultStartupTime;
        hyperspaceTime ??= DefaultTravelTime;

        var config = _dockSystem.GetDockingConfig(shuttleUid, target, priorityTag);
        hyperspace.StartupTime = startupTime.Value;
        hyperspace.TravelTime = hyperspaceTime.Value;
        hyperspace.StateTime = StartEndTime.FromStartDuration(
            _gameTiming.CurTime,
            TimeSpan.FromSeconds(hyperspace.StartupTime));
        hyperspace.PriorityTag = priorityTag;

        _console.RefreshShuttleConsoles(shuttleUid);

        // Valid dock for now time so just use that as the target.
        if (config != null)
        {
            hyperspace.TargetCoordinates = config.Coordinates;
            hyperspace.TargetAngle = config.Angle;
        }
        else if (TryGetFTLProximity(shuttleUid, new EntityCoordinates(target, Vector2.Zero), out var coords, out var targAngle))
        {
            hyperspace.TargetCoordinates = coords;
            hyperspace.TargetAngle = targAngle;
        }
        else
        {
            // FTL back to its own position.
            hyperspace.TargetCoordinates = Transform(shuttleUid).Coordinates;
            Log.Error($"Unable to FTL grid {ToPrettyString(shuttleUid)} to target properly?");
        }
    }

    private bool TrySetupFTL(EntityUid uid, ShuttleComponent shuttle, [NotNullWhen(true)] out FTLComponent? component)
    {
        component = null;

        if (HasComp<FTLComponent>(uid))
        {
            Log.Warning($"Tried queuing {ToPrettyString(uid)} which already has {nameof(FTLComponent)}?");
            return false;
        }

        _thruster.DisableLinearThrusters(shuttle);
        _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.North);
        _thruster.SetAngularThrust(shuttle, false);
        _dockSystem.UndockDocks(uid);

        component = AddComp<FTLComponent>(uid);
        component.State = FTLState.Starting;
        var audio = _audio.PlayPvs(_startupSound, uid);
        _audio.SetGridAudio(audio);
        component.StartupStream = audio?.Entity;

        // TODO: Play previs here for docking arrival.

        // Make sure the map is setup before we leave to avoid pop-in (e.g. parallax).
        EnsureFTLMap();
        return true;
    }

    /// <summary>
    /// Transitions shuttle to FTL map.
    /// </summary>
    private void UpdateFTLStarting(Entity<FTLComponent, ShuttleComponent> entity)
    {
        var uid = entity.Owner;
        var comp = entity.Comp1;
        var xform = _xformQuery.GetComponent(entity);
        DoTheDinosaur(xform);

        comp.State = FTLState.Travelling;
        var fromMapUid = xform.MapUid;
        var fromMatrix = _transform.GetWorldMatrix(xform);
        var fromRotation = _transform.GetWorldRotation(xform);

        var grid = Comp<MapGridComponent>(uid);
        var width = grid.LocalAABB.Width;
        var ftlMap = EnsureFTLMap();
        var body = _physicsQuery.GetComponent(entity);
        var shuttleCenter = grid.LocalAABB.Center;

        // Leave audio at the old spot
        // Just so we don't clip
        if (fromMapUid != null && TryComp(comp.StartupStream, out AudioComponent? startupAudio))
        {
            var clippedAudio = _audio.PlayStatic(_startupSound, Filter.Broadcast(),
                new EntityCoordinates(fromMapUid.Value, _mapSystem.GetGridPosition(entity.Owner)), true, startupAudio.Params);

            _audio.SetPlaybackPosition(clippedAudio, entity.Comp1.StartupTime);
            if (clippedAudio != null)
                clippedAudio.Value.Component.Flags |= AudioFlags.NoOcclusion;
        }

        // Offset the start by buffer range just to avoid overlap.
        var ftlStart = new EntityCoordinates(ftlMap, new Vector2(_index + width / 2f, 0f) - shuttleCenter);

        // Store the matrix for the grid prior to movement. This means any entities we need to leave behind we can make sure their positions are updated.
        // Setting the entity to map directly may run grid traversal (at least at time of writing this).
        var oldMapUid = xform.MapUid;
        var oldGridMatrix = _transform.GetWorldMatrix(xform);
        _transform.SetCoordinates(entity.Owner, ftlStart);
        LeaveNoFTLBehind((entity.Owner, xform), oldGridMatrix, oldMapUid);

        // Reset rotation so they always face the same direction.
        xform.LocalRotation = Angle.Zero;
        _index += width + Buffer;
        comp.StateTime = StartEndTime.FromCurTime(_gameTiming, comp.TravelTime - DefaultArrivalTime);

        Enable(uid, component: body);
        _physics.SetLinearVelocity(uid, new Vector2(0f, 20f), body: body);
        _physics.SetAngularVelocity(uid, 0f, body: body);
        _physics.SetLinearDamping(uid, body, 0f);
        _physics.SetAngularDamping(uid, body, 0f);

        _dockSystem.SetDockBolts(uid, true);
        _console.RefreshShuttleConsoles(uid);

        var ev = new FTLStartedEvent(uid, comp.TargetCoordinates, fromMapUid, fromMatrix, fromRotation);
        RaiseLocalEvent(uid, ref ev, true);

        // Audio
        var wowdio = _audio.PlayPvs(comp.TravelSound, uid);
        comp.TravelStream = wowdio?.Entity;
        _audio.SetGridAudio(wowdio);
    }

    /// <summary>
    /// Shuttle arriving.
    /// </summary>
    private void UpdateFTLTravelling(Entity<FTLComponent, ShuttleComponent> entity)
    {
        var shuttle = entity.Comp2;
        var comp = entity.Comp1;
        comp.StateTime = StartEndTime.FromCurTime(_gameTiming, DefaultArrivalTime);
        comp.State = FTLState.Arriving;

        if (entity.Comp1.VisualizerProto != null)
        {
            comp.VisualizerEntity = SpawnAtPosition(entity.Comp1.VisualizerProto, entity.Comp1.TargetCoordinates);
            var visuals = Comp<FtlVisualizerComponent>(comp.VisualizerEntity.Value);
            visuals.Grid = entity.Owner;
            Dirty(comp.VisualizerEntity.Value, visuals);
            _transform.SetLocalRotation(comp.VisualizerEntity.Value, entity.Comp1.TargetAngle);
            _pvs.AddGlobalOverride(comp.VisualizerEntity.Value);
        }

        _thruster.DisableLinearThrusters(shuttle);
        _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.South);

        _console.RefreshShuttleConsoles(entity.Owner);
    }

    /// <summary>
    ///  Shuttle arrived.
    /// </summary>
    private void UpdateFTLArriving(Entity<FTLComponent, ShuttleComponent> entity)
    {
        var uid = entity.Owner;
        var xform = _xformQuery.GetComponent(uid);
        var body = _physicsQuery.GetComponent(uid);
        var comp = entity.Comp1;
        DoTheDinosaur(xform);
        _dockSystem.SetDockBolts(entity, false);

        _physics.SetLinearVelocity(uid, Vector2.Zero, body: body);
        _physics.SetAngularVelocity(uid, 0f, body: body);
        _physics.SetLinearDamping(uid, body, entity.Comp2.LinearDamping);
        _physics.SetAngularDamping(uid, body, entity.Comp2.AngularDamping);

        var target = entity.Comp1.TargetCoordinates;

        MapId mapId;

        QueueDel(entity.Comp1.VisualizerEntity);
        entity.Comp1.VisualizerEntity = null;

        if (!Exists(entity.Comp1.TargetCoordinates.EntityId))
        {
            // Uhh good luck
            // Pick earliest map?
            var maps = EntityQuery<MapComponent>().Select(o => o.MapId).ToList();
            var map = maps.Min(o => o.GetHashCode());

            mapId = new MapId(map);
            TryFTLProximity(uid, _mapSystem.GetMap(mapId));
        }
        // Docking FTL
        else if (HasComp<MapGridComponent>(target.EntityId) &&
                 !HasComp<MapComponent>(target.EntityId))
        {
            var config = _dockSystem.GetDockingConfigAt(uid, target.EntityId, target, entity.Comp1.TargetAngle);
            var mapCoordinates = _transform.ToMapCoordinates(target);

            // Couldn't dock somehow so just fallback to regular position FTL.
            if (config == null)
            {
                TryFTLProximity(uid, target.EntityId);
            }
            else
            {
                FTLDock((uid, xform), config);
            }

            mapId = mapCoordinates.MapId;
        }
        // Position ftl
        else
        {
            // TODO: This should now use tryftlproximity
            mapId = _transform.GetMapId(target);
            _transform.SetCoordinates(uid, xform, target, rotation: entity.Comp1.TargetAngle);
        }

        if (_physicsQuery.TryGetComponent(uid, out body))
        {
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: body);
            _physics.SetAngularVelocity(uid, 0f, body: body);

            // Disable shuttle if it's on a planet; unfortunately can't do this in parent change messages due
            // to event ordering and awake body shenanigans (at least for now).
            if (HasComp<MapGridComponent>(xform.MapUid))
            {
                Disable(uid, component: body);
            }
            else
            {
                Enable(uid, component: body, shuttle: entity.Comp2);
            }
        }

        _thruster.DisableLinearThrusters(entity.Comp2);

        comp.TravelStream = _audio.Stop(comp.TravelStream);
        var audio = _audio.PlayPvs(_arrivalSound, uid);
        _audio.SetGridAudio(audio);

        if (TryComp<FTLDestinationComponent>(uid, out var dest))
        {
            dest.Enabled = true;
        }

        comp.State = FTLState.Cooldown;
        comp.StateTime = StartEndTime.FromCurTime(_gameTiming, FTLCooldown);
        _console.RefreshShuttleConsoles(uid);
        _mapManager.SetMapPaused(mapId, false);
        Smimsh(uid, xform: xform);

        var ftlEvent = new FTLCompletedEvent(uid, _mapSystem.GetMap(mapId));
        RaiseLocalEvent(uid, ref ftlEvent, true);
    }

    private void UpdateFTLCooldown(Entity<FTLComponent, ShuttleComponent> entity)
    {
        RemCompDeferred<FTLComponent>(entity);
        _console.RefreshShuttleConsoles(entity);
    }

    private void UpdateHyperspace()
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<FTLComponent, ShuttleComponent>();

        while (query.MoveNext(out var uid, out var comp, out var shuttle))
        {
            if (curTime < comp.StateTime.End)
                continue;

            var entity = (uid, comp, shuttle);

            switch (comp.State)
            {
                // Startup time has elapsed and in hyperspace.
                case FTLState.Starting:
                    UpdateFTLStarting(entity);
                    break;
                // Arriving, play effects
                case FTLState.Travelling:
                    UpdateFTLTravelling(entity);
                    break;
                // Arrived
                case FTLState.Arriving:
                    UpdateFTLArriving(entity);
                    break;
                case FTLState.Cooldown:
                    UpdateFTLCooldown(entity);
                    break;
                default:
                    Log.Error($"Found invalid FTL state {comp.State} for {uid}");
                    RemCompDeferred<FTLComponent>(uid);
                    break;
            }
        }
    }

    private float GetSoundRange(EntityUid uid)
    {
        if (!TryComp<MapGridComponent>(uid, out var grid))
            return 4f;

        return MathF.Max(grid.LocalAABB.Width, grid.LocalAABB.Height) + 12.5f;
    }

    /// <summary>
    /// Puts everyone unbuckled on the floor, paralyzed.
    /// </summary>
    private void DoTheDinosaur(TransformComponent xform)
    {
        // Get enumeration exceptions from people dropping things if we just paralyze as we go
        var toKnock = new ValueList<EntityUid>();
        KnockOverKids(xform, ref toKnock);
        TryComp<MapGridComponent>(xform.GridUid, out var grid);

        if (TryComp<PhysicsComponent>(xform.GridUid, out var shuttleBody))
        {
            foreach (var child in toKnock)
            {
                if (!_statusQuery.TryGetComponent(child, out var status))
                    continue;

                _stuns.TryParalyze(child, _hyperspaceKnockdownTime, true, status);

                // If the guy we knocked down is on a spaced tile, throw them too
                if (grid != null)
                    TossIfSpaced((xform.GridUid.Value, grid, shuttleBody), child);
            }
        }
    }

    private void LeaveNoFTLBehind(Entity<TransformComponent> grid, Matrix3x2 oldGridMatrix, EntityUid? oldMapUid)
    {
        if (oldMapUid == null)
            return;

        _noFtls.Clear();
        var oldGridRotation = oldGridMatrix.Rotation();
        _lookup.GetGridEntities(grid.Owner, _noFtls);

        foreach (var childUid in _noFtls)
        {
            if (!_xformQuery.TryComp(childUid, out var childXform))
                continue;

            // If we're not parented directly to the grid the matrix may be wrong.
            var relative = _physics.GetRelativePhysicsTransform(childUid.Owner, (grid.Owner, grid.Comp));

            _transform.SetCoordinates(
                childUid,
                childXform,
                new EntityCoordinates(oldMapUid.Value,
                Vector2.Transform(relative.Position, oldGridMatrix)), rotation: relative.Quaternion2D.Angle + oldGridRotation);
        }
    }

    private void KnockOverKids(TransformComponent xform, ref ValueList<EntityUid> toKnock)
    {
        // Not recursive because probably not necessary? If we need it to be that's why this method is separate.
        var childEnumerator = xform.ChildEnumerator;
        while (childEnumerator.MoveNext(out var child))
        {
            if (!_buckleQuery.TryGetComponent(child, out var buckle) || buckle.Buckled)
                continue;

            toKnock.Add(child);
        }
    }

    /// <summary>
    /// Throws people who are standing on a spaced tile, tries to throw them towards a neighbouring space tile
    /// </summary>
    private void TossIfSpaced(Entity<MapGridComponent, PhysicsComponent> shuttleEntity, EntityUid tossed)
    {
        var shuttleGrid = shuttleEntity.Comp1;
        var shuttleBody = shuttleEntity.Comp2;
        if (!_xformQuery.TryGetComponent(tossed, out var childXform))
            return;

        // only toss if its on lattice/space
        var tile = _mapSystem.GetTileRef(shuttleEntity, shuttleGrid, childXform.Coordinates);

        if (!tile.IsSpace(_tileDefManager))
            return;

        var throwDirection = childXform.LocalPosition - shuttleBody.LocalCenter;

        if (throwDirection == Vector2.Zero)
            return;

        _throwing.TryThrow(tossed, throwDirection.Normalized() * 10.0f, 50.0f);
    }

    /// <summary>
    /// Tries to dock with the target grid, otherwise falls back to proximity.
    /// This bypasses FTL travel time.
    /// </summary>
    public bool TryFTLDock(
        EntityUid shuttleUid,
        ShuttleComponent component,
        EntityUid targetUid,
        string? priorityTag = null)
    {
        return TryFTLDock(shuttleUid, component, targetUid, out _, priorityTag);
    }

    /// <summary>
    /// Tries to dock with the target grid, otherwise falls back to proximity.
    /// This bypasses FTL travel time.
    /// </summary>
    public bool TryFTLDock(
        EntityUid shuttleUid,
        ShuttleComponent component,
        EntityUid targetUid,
        [NotNullWhen(true)] out DockingConfig? config,
        string? priorityTag = null)
    {
        config = null;

        if (!_xformQuery.TryGetComponent(shuttleUid, out var shuttleXform) ||
            !_xformQuery.TryGetComponent(targetUid, out var targetXform) ||
            targetXform.MapUid == null ||
            !targetXform.MapUid.Value.IsValid())
        {
            return false;
        }

        config = _dockSystem.GetDockingConfig(shuttleUid, targetUid, priorityTag);

        if (config != null)
        {
            FTLDock((shuttleUid, shuttleXform), config);
            return true;
        }

        TryFTLProximity(shuttleUid, targetUid, shuttleXform, targetXform);
        return false;
    }

    /// <summary>
    /// Forces an FTL dock.
    /// </summary>
    public void FTLDock(Entity<TransformComponent> shuttle, DockingConfig config)
    {
        // Set position
        var mapCoordinates = _transform.ToMapCoordinates(config.Coordinates);
        var mapUid = _mapSystem.GetMap(mapCoordinates.MapId);
        _transform.SetCoordinates(shuttle.Owner, shuttle.Comp, new EntityCoordinates(mapUid, mapCoordinates.Position), rotation: config.Angle);

        // Connect everything
        foreach (var (dockAUid, dockBUid, dockA, dockB) in config.Docks)
        {
            _dockSystem.Dock((dockAUid, dockA), (dockBUid, dockB));
        }
    }

    /// <summary>
    /// Tries to get the target position to FTL near the target coordinates.
    /// If the target coordinates have a mapgrid then will try to offset the AABB.
    /// </summary>
    /// <param name="minOffset">Min offset for the final FTL.</param>
    /// <param name="maxOffset">Max offset for the final FTL from the box we spawn.</param>
    private bool TryGetFTLProximity(
        EntityUid shuttleUid,
        EntityCoordinates targetCoordinates,
        out EntityCoordinates coordinates, out Angle angle,
        float minOffset = 0f, float maxOffset = 64f,
        TransformComponent? xform = null, TransformComponent? targetXform = null)
    {
        DebugTools.Assert(minOffset < maxOffset);
        coordinates = EntityCoordinates.Invalid;
        angle = Angle.Zero;

        if (!Resolve(targetCoordinates.EntityId, ref targetXform) ||
            targetXform.MapUid == null ||
            !targetXform.MapUid.Value.IsValid() ||
            !Resolve(shuttleUid, ref xform))
        {
            return false;
        }

        // We essentially expand the Box2 of the target area until nothing else is added then we know it's valid.
        // Can't just get an AABB of every grid as we may spawn very far away.
        var nearbyGrids = new HashSet<EntityUid>();
        var shuttleAABB = Comp<MapGridComponent>(shuttleUid).LocalAABB;

        // Start with small point.
        // If our target pos is offset we mot even intersect our target's AABB so we don't include it.
        var targetLocalAABB = Box2.CenteredAround(targetCoordinates.Position, Vector2.One);

        // How much we expand the target AABB be.
        // We half it because we only need the width / height in each direction if it's placed at a particular spot.
        var expansionAmount = MathF.Max(shuttleAABB.Width / 2f, shuttleAABB.Height / 2f);

        // Expand the starter AABB so we have something to query to start with.
        var targetAABB = _transform.GetWorldMatrix(targetXform)
            .TransformBox(targetLocalAABB)
            .Enlarged(expansionAmount);

        var iteration = 0;
        var lastCount = nearbyGrids.Count;
        var mapId = targetXform.MapID;
        var grids = new List<Entity<MapGridComponent>>();

        while (iteration < FTLProximityIterations)
        {
            grids.Clear();
            // We pass in an expanded offset here so we can safely do a random offset later.
            // We don't include this in the actual targetAABB because then we would be double-expanding it.
            // Once in this loop, then again when placing the shuttle later.
            // Note that targetAABB already has expansionAmount factored in already.
            _mapManager.FindGridsIntersecting(mapId, targetAABB.Enlarged(maxOffset), ref grids);

            foreach (var grid in grids)
            {
                if (!nearbyGrids.Add(grid))
                    continue;

                // Include the other grid's AABB (expanded by ours) as well.
                targetAABB = targetAABB.Union(
                    _transform.GetWorldMatrix(grid)
                    .TransformBox(Comp<MapGridComponent>(grid).LocalAABB.Enlarged(expansionAmount)));
            }

            // Can do proximity
            if (nearbyGrids.Count == lastCount)
            {
                break;
            }

            iteration++;
            lastCount = nearbyGrids.Count;

            // Mishap moment, dense asteroid field or whatever
            if (iteration != FTLProximityIterations)
                continue;

            var query = AllEntityQuery<MapGridComponent>();
            while (query.MoveNext(out var uid, out var grid))
            {
                // Don't add anymore as it is irrelevant, but that doesn't mean we need to re-do existing work.
                if (nearbyGrids.Contains(uid))
                    continue;

                targetAABB = targetAABB.Union(
                    _transform.GetWorldMatrix(uid)
                    .TransformBox(Comp<MapGridComponent>(uid).LocalAABB.Enlarged(expansionAmount)));
            }

            break;
        }

        // Now we have a targetAABB. This has already been expanded to account for our fat ass.
        Vector2 spawnPos;

        if (TryComp<PhysicsComponent>(shuttleUid, out var shuttleBody))
        {
            _physics.SetLinearVelocity(shuttleUid, Vector2.Zero, body: shuttleBody);
            _physics.SetAngularVelocity(shuttleUid, 0f, body: shuttleBody);
        }

        // TODO: This should prefer the position's angle instead.
        // TODO: This is pretty crude for multiple landings.
        if (nearbyGrids.Count > 1 || !HasComp<MapComponent>(targetXform.GridUid))
        {
            // Pick a random angle
            var offsetAngle = _random.NextAngle();

            // Our valid spawn positions are <targetAABB width / height +  offset> away.
            var minRadius = MathF.Max(targetAABB.Width / 2f, targetAABB.Height / 2f);
            spawnPos = targetAABB.Center + offsetAngle.RotateVec(new Vector2(_random.NextFloat(minRadius + minOffset, minRadius + maxOffset), 0f));
        }
        else if (shuttleBody != null)
        {
            (spawnPos, angle) = _transform.GetWorldPositionRotation(targetXform);
        }
        else
        {
            spawnPos = _transform.GetWorldPosition(targetXform);
        }

        var offset = Vector2.Zero;

        // Offset it because transform does not correspond to AABB position.
        if (TryComp(shuttleUid, out MapGridComponent? shuttleGrid))
        {
            offset = -shuttleGrid.LocalAABB.Center;
        }

        if (!HasComp<MapComponent>(targetXform.GridUid))
        {
            angle = _random.NextAngle();
        }
        else
        {
            angle = Angle.Zero;
        }

        // Rotate our localcenter around so we spawn exactly where we "think" we should (center of grid on the dot).
        var transform = new Transform(spawnPos, angle);
        spawnPos = Robust.Shared.Physics.Transform.Mul(transform, offset);

        coordinates = new EntityCoordinates(targetXform.MapUid.Value, spawnPos - offset);
        return true;
    }

    /// <summary>
    /// Tries to arrive nearby without overlapping with other grids.
    /// </summary>
    public bool TryFTLProximity(EntityUid shuttleUid, EntityUid targetUid, TransformComponent? xform = null, TransformComponent? targetXform = null)
    {
        if (!Resolve(targetUid, ref targetXform) ||
            targetXform.MapUid == null ||
            !targetXform.MapUid.Value.IsValid() ||
            !Resolve(shuttleUid, ref xform))
        {
            return false;
        }

        if (!TryGetFTLProximity(shuttleUid, new EntityCoordinates(targetUid, Vector2.Zero), out var coords, out var angle, xform: xform, targetXform: targetXform))
            return false;

        _transform.SetCoordinates(shuttleUid, xform, coords, rotation: angle);
        return true;
    }

    /// <summary>
    /// Tries to FTL to the target coordinates; will move nearby if not possible.
    /// </summary>
    public bool TryFTLProximity(Entity<TransformComponent?> shuttle, EntityCoordinates targetCoordinates)
    {
        if (!Resolve(shuttle.Owner, ref shuttle.Comp) ||
            _transform.GetMap(targetCoordinates)?.IsValid() != true)
        {
            return false;
        }

        if (!TryGetFTLProximity(shuttle, targetCoordinates, out var coords, out var angle))
            return false;

        _transform.SetCoordinates(shuttle, shuttle.Comp, coords, rotation: angle);
        return true;
    }

    /// <summary>
    /// Flattens / deletes everything under the grid upon FTL.
    /// </summary>
    private void Smimsh(EntityUid uid, FixturesComponent? manager = null, MapGridComponent? grid = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref manager, ref grid, ref xform) || xform.MapUid == null)
            return;

        if (!TryComp(xform.MapUid, out BroadphaseComponent? lookup))
            return;

        // Flatten anything not parented to a grid.
        var transform = _physics.GetRelativePhysicsTransform((uid, xform), xform.MapUid.Value);
        var aabbs = new List<Box2>(manager.Fixtures.Count);
        var tileSet = new List<(Vector2i, Tile)>();

        foreach (var fixture in manager.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            var aabb = fixture.Shape.ComputeAABB(transform, 0);

            // Shift it slightly
            // Create a small border around it.
            aabb = aabb.Enlarged(0.2f);
            aabbs.Add(aabb);

            // Handle clearing biome stuff as relevant.
            tileSet.Clear();
            _biomes.ReserveTiles(xform.MapUid.Value, aabb, tileSet);
            _lookupEnts.Clear();
            _immuneEnts.Clear();
            // TODO: Ideally we'd query first BEFORE moving grid but needs adjustments above.
            _lookup.GetLocalEntitiesIntersecting(xform.MapUid.Value, fixture.Shape, transform, _lookupEnts, flags: LookupFlags.Uncontained, lookup: lookup);

            foreach (var ent in _lookupEnts)
            {
                if (ent == uid || _immuneEnts.Contains(ent))
                {
                    continue;
                }

                // If it's on our grid ignore it.
                if (!_xformQuery.TryComp(ent, out var childXform) || childXform.GridUid == uid)
                {
                    continue;
                }

                if (_immuneQuery.HasComponent(ent))
                {
                    continue;
                }

                if (_bodyQuery.TryGetComponent(ent, out var mob))
                {
                    _logger.Add(LogType.Gib, LogImpact.Extreme, $"{ToPrettyString(ent):player} got gibbed by the shuttle" +
                                                                $" {ToPrettyString(uid)} arriving from FTL at {xform.Coordinates:coordinates}");
                    var gibs = _bobby.GibBody(ent, body: mob);
                    _immuneEnts.UnionWith(gibs);
                    continue;
                }

                QueueDel(ent);
            }
        }

        var ev = new ShuttleFlattenEvent(xform.MapUid.Value, aabbs);
        RaiseLocalEvent(ref ev);
    }
}
