using System.Diagnostics.CodeAnalysis;
using Content.Server.Buckle.Components;
using Content.Server.Doors.Components;
using Content.Server.Doors.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Stunnable;
using Content.Shared.Sound;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    /*
     * This is a way to move a shuttle from one location to another, via an intermediate map for fanciness.
     */

    [Dependency] private readonly DoorSystem _doors = default!;
    [Dependency] private readonly StunSystem _stuns = default!;

    private MapId? _hyperSpaceMap;

    private const float DefaultStartupTime = 5.5f;
    private const float DefaultTravelTime = 30f;

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
    /// Moves a shuttle from its current position to the target one. Goes through the hyperspace map while the timer is running.
    /// </summary>
    public void Hyperspace(ShuttleComponent component,
        EntityCoordinates coordinates,
        float startupTime = DefaultStartupTime,
        float hyperspaceTime = DefaultTravelTime)
    {
        if (!TrySetupHyperspace(component.Owner, out var hyperspace))
           return;

        hyperspace.StartupTime = startupTime;
        hyperspace.TravelTime = hyperspaceTime;
        hyperspace.Accumulator = hyperspace.StartupTime;
        hyperspace.TargetCoordinates = coordinates;
    }

    /// <summary>
    /// Moves a shuttle from its current position to docked on the target one. Goes through the hyperspace map while the timer is running.
    /// </summary>
    public void Hyperspace(ShuttleComponent component,
        EntityUid target,
        float startupTime = DefaultStartupTime,
        float hyperspaceTime = DefaultTravelTime)
    {
        if (!TrySetupHyperspace(component.Owner, out var hyperspace))
            return;

        hyperspace.StartupTime = startupTime;
        hyperspace.TravelTime = hyperspaceTime;
        hyperspace.Accumulator = hyperspace.StartupTime;
        hyperspace.TargetUid = target;
    }

    private bool TrySetupHyperspace(EntityUid uid, [NotNullWhen(true)] out HyperspaceComponent? component)
    {
        component = null;

        if (HasComp<HyperspaceComponent>(uid))
        {
            _sawmill.Warning($"Tried queuing {ToPrettyString(uid)} which already has HyperspaceComponent?");
            return false;
        }

        // TODO: Maybe move this to docking instead?
        SetDocks(uid, false);

        component = AddComp<HyperspaceComponent>(uid);
        // TODO: Need BroadcastGrid to not be bad.
        SoundSystem.Play(_startupSound.GetSound(), Filter.Pvs(component.Owner, GetSoundRange(component.Owner), entityManager: EntityManager), _startupSound.Params);
        return true;
    }

    private void UpdateHyperspace(float frameTime)
    {
        foreach (var comp in EntityQuery<HyperspaceComponent>())
        {
            comp.Accumulator -= frameTime;

            if (comp.Accumulator > 0f) continue;

            var xform = Transform(comp.Owner);
            PhysicsComponent? body;

            switch (comp.State)
            {
                // Startup time has elapsed and in hyperspace.
                case HyperspaceState.Starting:
                    DoTheDinosaur(xform);

                    comp.State = HyperspaceState.Travelling;
                    SetupHyperspace();

                    var width = Comp<IMapGridComponent>(comp.Owner).Grid.LocalAABB.Width;
                    xform.Coordinates = new EntityCoordinates(_mapManager.GetMapEntityId(_hyperSpaceMap!.Value), new Vector2(_index + width / 2f, 0f));
                    xform.LocalRotation = Angle.Zero;
                    _index += width + Buffer;
                    comp.Accumulator += comp.TravelTime;

                    if (TryComp(comp.Owner, out body))
                    {
                        body.LinearVelocity = new Vector2(0f, 20f);
                        body.AngularVelocity = 0f;
                        body.LinearDamping = 0f;
                        body.AngularDamping = 0f;
                    }

                    SetDockBolts(comp.Owner, true);

                    break;
                // Arrive.
                case HyperspaceState.Travelling:
                    DoTheDinosaur(xform);
                    SetDockBolts(comp.Owner, false);
                    SetDocks(comp.Owner, true);

                    if (TryComp(comp.Owner, out body))
                    {
                        body.LinearVelocity = Vector2.Zero;
                        body.AngularVelocity = 0f;
                        body.LinearDamping = ShuttleIdleLinearDamping;
                        body.AngularDamping = ShuttleIdleAngularDamping;
                    }

                    if (comp.TargetUid != null && TryComp<ShuttleComponent>(comp.Owner, out var shuttle))
                    {
                        TryHyperspaceDock(shuttle, comp.TargetUid.Value);
                    }
                    else
                    {
                        xform.Coordinates = comp.TargetCoordinates;
                    }

                    SoundSystem.Play(_arrivalSound.GetSound(),
                        Filter.Pvs(comp.Owner, GetSoundRange(comp.Owner), entityManager: EntityManager));
                    RemComp<HyperspaceComponent>(comp.Owner);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
            door.SetBoltsWithAudio(enabled);
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

    private bool TryHyperspaceDock(ShuttleComponent component, EntityUid targetUid)
    {
        if (!TryComp<TransformComponent>(component.Owner, out var xform) ||
            !TryComp<TransformComponent>(targetUid, out var targetXform)) return false;

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

        var shuttleAABB = Comp<IMapGridComponent>(component.Owner).Grid.WorldAABB;
        Box2? aabb = null;

        // Spawn nearby.
        foreach (var grid in _mapManager.GetAllMapGrids(targetXform.MapID))
        {
            var gridAABB = grid.WorldAABB;
            aabb = aabb?.Union(gridAABB) ?? gridAABB;
        }

        aabb ??= new Box2();

        var minRadius = MathF.Max(aabb.Value.Width, aabb.Value.Height) + MathF.Max(shuttleAABB.Width, shuttleAABB.Height);
        var spawnPos = aabb.Value.Center + _random.NextVector2(minRadius, minRadius + 10f);

        if (TryComp<PhysicsComponent>(component.Owner, out var shuttleBody))
        {
            shuttleBody.LinearVelocity = Vector2.Zero;
            shuttleBody.AngularVelocity = 0f;
        }

        xform.WorldPosition = spawnPos;
        xform.WorldRotation = _random.NextAngle();
        return false;
    }
}
