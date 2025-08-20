using Content.Shared._Starlight.MassDriver.Components;
using Content.Shared._Starlight.MassDriver;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using System.Linq;
using Content.Shared.Power;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking.Events;
using Content.Server.Power.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Starlight.MassDriver.EntitySystems;

public sealed class MassDriverSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming Timing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MassDriverComponent, PowerChangedEvent>(OnPowerChanged);

        // UI for console -_-
        SubscribeLocalEvent<MassDriverConsoleComponent, ComponentInit>(OnInit); // Update state on init
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverModeMessage>(OnModeChanged);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverLaunchMessage>(OnLaunch);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverThrowSpeedMessage>(OnThrowSpeedChanged);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverThrowDistanceMessage>(OnThrowDistanceChanged);

        // Device Linking
        SubscribeLocalEvent<MassDriverConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<MassDriverConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<MassDriverComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<MassDriverConsoleComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
    }

    #region DeviceLinking

    private void OnNewLink(EntityUid uid, MassDriverConsoleComponent component, NewLinkEvent args)
    {
        if (!TryComp<MassDriverComponent>(args.Sink, out var driver))
            return;

        component.MassDriver = GetNetEntity(args.Sink);
        driver.Console = GetNetEntity(uid);

        Dirty(args.Sink, driver);
        Dirty(uid, component);

        UpdateUserInterface(uid, args.Sink, driver);
    }

    private void OnPortDisconnected(EntityUid uid, MassDriverConsoleComponent component, PortDisconnectedEvent args)
    {
        var massDriverNetEntity = component.MassDriver;
        if (args.Port != component.LinkingPort || massDriverNetEntity == null)
            return;

        var massDriverEntityUid = GetEntity(massDriverNetEntity);
        if (TryComp<MassDriverComponent>(massDriverEntityUid, out var massDriverComponent))
        {
            massDriverComponent.Console = null;
            Dirty(massDriverEntityUid.Value, massDriverComponent);
        }

        component.MassDriver = null;
        Dirty(uid, component);
    }

    private void OnSignalReceived(EntityUid uid, MassDriverComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.LaunchPort && component.Mode == MassDriverMode.Manual)
            AddComp<ActiveMassDriverComponent>(uid);
    }

    #endregion

    #region Logic

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveMassDriverComponent, MassDriverComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var activeMassDriver, out var massDriver, out var powered))
        {
            if (Timing.CurTime < activeMassDriver.NextUpdateTime)
                continue;

            if (activeMassDriver.NextUpdateTime == TimeSpan.Zero)
            {
                activeMassDriver.NextUpdateTime = Timing.CurTime + activeMassDriver.UpdateDelay;
                continue;
            }
            activeMassDriver.NextUpdateTime = Timing.CurTime + activeMassDriver.UpdateDelay;

            if (!_powerReceiver.IsPowered(uid))
                continue;

            var entities = new HashSet<EntityUid>();
            _lookup.GetEntitiesIntersecting(uid, entities);
            var entitiesCount = entities.Count(a => !Transform(a).Anchored);

            if (entitiesCount == 0)
            {
                if (activeMassDriver.NextThrowTime != TimeSpan.Zero)
                {
                    activeMassDriver.NextThrowTime = TimeSpan.Zero;
                    _appearance.SetData(uid, MassDriverVisuals.Launching, false);
                    _powerReceiver.SetLoad(powered, powered.Load - massDriver.LaunchPowerLoad);
                }
                if (massDriver.Mode == MassDriverMode.Manual)
                    RemComp<ActiveMassDriverComponent>(uid);
                continue;
            }

            if (activeMassDriver.NextThrowTime == TimeSpan.Zero)
            {
                activeMassDriver.NextThrowTime = Timing.CurTime + massDriver.ThrowDelay;
                continue;
            }
            else if (Timing.CurTime < activeMassDriver.NextThrowTime)
                continue;

            _powerReceiver.SetLoad(powered, powered.Load + massDriver.LaunchPowerLoad);
            _appearance.SetData(uid, MassDriverVisuals.Launching, true);

            var xform = Transform(uid);
            var throwing = xform.LocalRotation.ToWorldVec() * (massDriver.CurrentThrowDistance - (massDriver.ThrowCountDelta * (entities.Count - 1)));
            var direction = xform.Coordinates.Offset(throwing);
            var speed = massDriver.CurrentThrowSpeed - (massDriver.ThrowCountDelta * (entitiesCount - 1));

            foreach (var entity in entities)
                _throwing.TryThrow(entity, direction, speed);

            _audioSystem.PlayPvs(massDriver.LaunchSound, uid);
        }
    }

    #endregion

    #region UI

    private void OnInit(EntityUid uid, MassDriverConsoleComponent massDriverConsole, ComponentInit args)
    {
        if (massDriverConsole.MassDriver != null)
        {
            var massDriverUid = GetEntity(massDriverConsole.MassDriver);
            if (TryComp<MassDriverComponent>(massDriverUid, out var component))
                UpdateUserInterface(uid, massDriverUid.Value, component);
        }
        else
            UpdateUserInterface(uid, null, null);
    }

    private void OnBoundUiOpened(EntityUid uid, MassDriverConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (component.MassDriver != null)
        {
            var massDriverUid = GetEntity(component.MassDriver);
            if (TryComp<MassDriverComponent>(massDriverUid, out var driver))
                UpdateUserInterface(uid, massDriverUid.Value, driver);
        }
        else
            UpdateUserInterface(uid, null, null);
    }

    private void OnModeChanged(EntityUid uid, MassDriverConsoleComponent massDriverConsole, MassDriverModeMessage args)
    {
        if (massDriverConsole.MassDriver != null)
        {
            var massDriverUid = GetEntity(massDriverConsole.MassDriver);
            if (TryComp<MassDriverComponent>(massDriverUid, out var massDriverComponent))
            {
                massDriverComponent.Mode = args.Mode;
                Dirty(massDriverUid.Value, massDriverComponent);

                if (massDriverComponent.Mode == MassDriverMode.Auto)
                    EnsureComp<ActiveMassDriverComponent>(massDriverUid.Value);
                else if (HasComp<ActiveMassDriverComponent>(massDriverUid.Value))
                    RemComp<ActiveMassDriverComponent>(massDriverUid.Value);

                UpdateUserInterface(uid, massDriverUid.Value, massDriverComponent);
            }
        }
    }

    private void OnLaunch(EntityUid uid, MassDriverConsoleComponent massDriverConsole, MassDriverLaunchMessage args)
    {
        var massDriverUid = GetEntity(massDriverConsole.MassDriver);
        if (massDriverUid != null)
            AddComp<ActiveMassDriverComponent>(massDriverUid.Value);
    }

    private void OnThrowSpeedChanged(EntityUid uid, MassDriverConsoleComponent massDriverConsole, MassDriverThrowSpeedMessage args)
    {
        var massDriverUid = GetEntity(massDriverConsole.MassDriver);
        if (massDriverUid != null && TryComp<MassDriverComponent>(massDriverUid, out var massDriverComponent))
            massDriverComponent.CurrentThrowSpeed = Math.Clamp(Normalize(args.Speed), massDriverComponent.MinThrowSpeed, massDriverComponent.MaxThrowSpeed);
    }

    private void OnThrowDistanceChanged(EntityUid uid, MassDriverConsoleComponent massDriverConsole, MassDriverThrowDistanceMessage args)
    {
        var massDriverUid = GetEntity(massDriverConsole.MassDriver);
        if (massDriverUid != null && TryComp<MassDriverComponent>(massDriverUid, out var massDriverComponent))
            massDriverComponent.CurrentThrowDistance = Math.Clamp(Normalize(args.Distance), massDriverComponent.MinThrowDistance, massDriverComponent.MaxThrowDistance);
    }

    private void UpdateUserInterface(EntityUid console, EntityUid? massDriver, MassDriverComponent? component = null)
    {
        if (!_ui.HasUi(console, MassDriverConsoleUiKey.Key))
            return;

        MassDriverUiState state;

        if (massDriver != null && Resolve(massDriver.Value, ref component))
        {
            state = new MassDriverUiState
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
            state = new MassDriverUiState
            {
                MassDriverLinked = false
            };
        }

        _ui.SetUiState(console, MassDriverConsoleUiKey.Key, state);
    }

    private float Normalize(float value, int decimals = 1)
    {
        return (float)Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }
    
    #endregion
}