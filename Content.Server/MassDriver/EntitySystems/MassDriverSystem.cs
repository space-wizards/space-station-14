using Content.Shared.MassDriver.Components;
using Content.Shared.MassDriver;
using Content.Shared.Throwing;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Power;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking.Events;
using Content.Server.Power.Components;
using Content.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Server.MassDriver.EntitySystems;

public sealed class MassDriverSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MassDriverComponent, PowerChangedEvent>(OnPowerChanged);

        // UI for console -_-
        SubscribeLocalEvent<MassDriverConsoleComponent, ComponentInit>(OnInit); // Update state on init
        SubscribeLocalEvent<MassDriverConsoleComponent, BoundUIOpenedEvent>(OnBoundUiOpened); // Update state on ui open
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverModeMessage>(OnModeChanged);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverLaunchMessage>(OnLaunch);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverThrowSpeedMessage>(OnThrowSpeedChanged);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverThrowDistanceMessage>(OnThrowDistanceChanged);

        // Device Linking
        SubscribeLocalEvent<MassDriverConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<MassDriverConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<MassDriverComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    #region DeviceLinking

    /// <summary>
    /// Handle new link to add mass driver in mass driver console component
    /// </summary>
    /// <param name="uid">Mass Driver Console</param>
    /// <param name="component">Mass Driver Console Component</param>
    /// <param name="args">Event arguments</param>
    private void OnNewLink(EntityUid uid, MassDriverConsoleComponent component, NewLinkEvent args)
    {
        if (!TryComp<MassDriverComponent>(args.Sink, out var driver))
            return;

        component.MassDrivers.Add(args.Sink);

        Dirty(args.Sink, driver);
        Dirty(uid, component);

        UpdateUserInterface(uid, component.MassDrivers.FirstOrNull());
    }

    /// <summary>
    /// Handle disconnecting to remove mass driver from mass driver console component
    /// </summary>
    /// <param name="uid">Mass Driver Console</param>
    /// <param name="component">Mass Driver Console Component</param>
    /// <param name="args">Event arguments</param>
    private void OnPortDisconnected(EntityUid uid, MassDriverConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port != component.LinkingPort || !component.MassDrivers.Contains(args.Sink))
            return;

        component.MassDrivers.Remove(args.Sink);
        Dirty(uid, component);
    }

    /// <summary>
    /// Handle signal receive, for launch some objects.
    /// </summary>
    /// <param name="uid">Mass Driver</param>
    /// <param name="component">Mass Driver Component</param>
    /// <param name="args">Event arguments</param>
    private void OnSignalReceived(EntityUid uid, MassDriverComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.LaunchPort && component.Mode == MassDriverMode.Manual)
            AddComp<ActiveMassDriverComponent>(uid);
    }

    #endregion

    #region Logic

    /// <summary>
    /// Handle power changing to disable or enable mass driver, so it can't work without power
    /// </summary>
    /// <param name="uid">Mass Driver</param>
    /// <param name="component">Mass Driver Component</param>
    /// <param name="args">Event arguments</param>
    private void OnPowerChanged(EntityUid uid, MassDriverComponent component, ref PowerChangedEvent args)
    {
        if (component.Mode != MassDriverMode.Auto)
            return;

        var hasComp = HasComp<ActiveMassDriverComponent>(uid);
        if (hasComp && !args.Powered)
            RemComp<ActiveMassDriverComponent>(uid);
        else if (!hasComp && args.Powered)
            EnsureComp<ActiveMassDriverComponent>(uid);
    }

    /// <summary>
    /// Update only active mass drivers, so we have it more optimized than conveyors
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveMassDriverComponent, MassDriverComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var activeMassDriver, out var massDriver, out var powered))
        {
            if (_timing.CurTime < activeMassDriver.NextUpdateTime)
                continue;

            if (activeMassDriver.NextUpdateTime == TimeSpan.Zero)
            {
                activeMassDriver.NextUpdateTime = _timing.CurTime + activeMassDriver.UpdateDelay;
                continue;
            }
            activeMassDriver.NextUpdateTime = _timing.CurTime + activeMassDriver.UpdateDelay;

            var entities = new HashSet<EntityUid>();
            _lookup.GetEntitiesIntersecting(uid, entities);
            var entitiesCount = entities.Count(a => !Transform(a).Anchored);

            if (entitiesCount == 0)
            {
                // Disable mass driver if we throw all entities
                if (activeMassDriver.NextThrowTime != TimeSpan.Zero)
                {
                    if (TryComp<AmbientSoundComponent>(uid, out var ambient))
                        _audioSystem.SetAmbience(uid, false, ambient);
                    activeMassDriver.NextThrowTime = TimeSpan.Zero;
                    _appearance.SetData(uid, MassDriverVisuals.Launching, false);
                    _powerReceiver.SetLoad(powered, massDriver.MassDriverPowerLoad);
                }
                if (massDriver.Mode == MassDriverMode.Manual)
                    RemComp<ActiveMassDriverComponent>(uid);
                continue;
            }

            // If we find first entity, charge mass driver(wait n seconds setuped in ThrowDelay)
            if (activeMassDriver.NextThrowTime == TimeSpan.Zero)
            {
                activeMassDriver.NextThrowTime = _timing.CurTime + massDriver.ThrowDelay;
                continue;
            }
            else if (_timing.CurTime < activeMassDriver.NextThrowTime)
                continue;

            _powerReceiver.SetLoad(powered, massDriver.LaunchPowerLoad);
            _appearance.SetData(uid, MassDriverVisuals.Launching, true);

            ThrowEntities(uid, massDriver, entities, entitiesCount);

            if (TryComp<AmbientSoundComponent>(uid, out var ambientSound))
                _audioSystem.SetAmbience(uid, true, ambientSound);
        }
    }

    /// <summary>
    /// Throws All entities in list.
    /// </summary>
    /// <param name="massDriver">Mass Driver</param>
    /// <param name="massDriverComponent">Mass Driver Component</param>
    /// <param name="targets">Targets List</param>
    /// <param name="targetCount">Count of target(added, because we can ignore some targets like anchored, etc.)</param>
    private void ThrowEntities(EntityUid massDriver, MassDriverComponent massDriverComponent, HashSet<EntityUid> targets, int targetCount)
    {
        var xform = Transform(massDriver);
        var throwing = xform.LocalRotation.ToWorldVec() * (massDriverComponent.CurrentThrowDistance - (massDriverComponent.ThrowCountDelta * (targets.Count - 1)));
        var direction = xform.Coordinates.Offset(throwing);
        var speed = massDriverComponent.Hacked ? massDriverComponent.HackedSpeedRewrite : massDriverComponent.CurrentThrowSpeed - (massDriverComponent.ThrowCountDelta * (targetCount - 1));

        foreach (var entity in targets)
            _throwing.TryThrow(entity, direction, speed);
    }

    #endregion

    #region UI

    /// <summary>
    /// Update ui on init
    /// </summary>
    private void OnInit(EntityUid uid, MassDriverConsoleComponent component, ComponentInit args)
    {
        UpdateUserInterface(uid, component.MassDrivers.FirstOrNull());
    }

    /// <summary>
    /// Update ui on ui open
    /// </summary>
    private void OnBoundUiOpened(EntityUid uid, MassDriverConsoleComponent component, BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, component.MassDrivers.FirstOrNull());
    }

    /// <summary>
    /// Handle mode changing
    /// </summary>
    private void OnModeChanged(EntityUid uid, MassDriverConsoleComponent component, MassDriverModeMessage args)
    {
        foreach (var massDriverNetEntity in component.MassDrivers)
        {
            var massDriverUid = massDriverNetEntity;

            if (!TryComp<MassDriverComponent>(massDriverUid, out var massDriverComponent))
                continue;

            massDriverComponent.Mode = args.Mode;
            Dirty(massDriverUid, massDriverComponent);

            if (massDriverComponent.Mode == MassDriverMode.Auto)
                EnsureComp<ActiveMassDriverComponent>(massDriverUid);
            RemComp<ActiveMassDriverComponent>(massDriverUid);
        }

        UpdateUserInterface(uid, component.MassDrivers.FirstOrNull());
    }

    /// <summary>
    /// Handle launch button, so we can launch entity
    /// </summary>
    private void OnLaunch(EntityUid uid, MassDriverConsoleComponent component, MassDriverLaunchMessage args)
    {
        foreach (var massDriverUid in component.MassDrivers)
            AddComp<ActiveMassDriverComponent>(massDriverUid);
    }

    /// <summary>
    /// Handle throw speed slider
    /// </summary>
    private void OnThrowSpeedChanged(EntityUid uid, MassDriverConsoleComponent component, MassDriverThrowSpeedMessage args)
    {
        foreach (var massDriverUid in component.MassDrivers)
            if (TryComp<MassDriverComponent>(massDriverUid, out var massDriverComponent))
                massDriverComponent.CurrentThrowSpeed = Math.Clamp(args.Speed, massDriverComponent.MinThrowSpeed, massDriverComponent.MaxThrowSpeed);
    }

    /// <summary>
    /// Handle throw distance slider
    /// </summary>
    private void OnThrowDistanceChanged(EntityUid uid, MassDriverConsoleComponent component, MassDriverThrowDistanceMessage args)
    {
        foreach (var massDriverUid in component.MassDrivers)
            if (TryComp<MassDriverComponent>(massDriverUid, out var massDriverComponent))
                massDriverComponent.CurrentThrowDistance = Math.Clamp(args.Distance, massDriverComponent.MinThrowDistance, massDriverComponent.MaxThrowDistance);
    }

    private void UpdateUserInterface(EntityUid console, EntityUid? massDriver, MassDriverComponent? component = null)
    {
        if (!_ui.HasUi(console, MassDriverConsoleUiKey.Key))
            return;

        MassDriverComponentState state;

        if (massDriver != null && Resolve(massDriver.Value, ref component))
        {
            state = new MassDriverComponentState
            {
                MaxThrowSpeed = component.MaxThrowSpeed,
                MaxThrowDistance = component.MaxThrowDistance,
                MinThrowSpeed = component.MinThrowSpeed,
                MinThrowDistance = component.MinThrowDistance,
                CurrentThrowSpeed = component.CurrentThrowSpeed,
                CurrentThrowDistance = component.CurrentThrowDistance,
                CurrentMassDriverMode = component.Mode,
                MassDriverLinked = true

            };
        }
        else
        {
            state = new MassDriverComponentState
            {
                MassDriverLinked = false
            };
        }

        _ui.ServerSendUiMessage(console, MassDriverConsoleUiKey.Key, new MassDriverUpdateUIMessage(state));
    }

    #endregion
}
