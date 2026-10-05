using Content.Server.DeviceLinking.Systems;
using Content.Server.Power.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Events;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// handles device link signals for power devices, such as APC, SMES units and substations
/// </summary>
public sealed partial class PowerDeviceSignalControlSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private ApcSystem _apc = default!;
    [Dependency] private DeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ApcSignalControlComponent, MapInitEvent>(OnApcMapInit);
        SubscribeLocalEvent<ApcSignalControlComponent, SignalReceivedEvent>(OnApcSignalReceived);

        SubscribeLocalEvent<BatterySignalControlComponent, MapInitEvent>(OnBatteryMapInit);
        SubscribeLocalEvent<BatterySignalControlComponent, SignalReceivedEvent>(OnBatterySignalReceived);
    }

    private void OnApcMapInit(Entity<ApcSignalControlComponent> ent, ref MapInitEvent args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.ToggleOutputPort);
    }

    private void OnApcSignalReceived(Entity<ApcSignalControlComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.ToggleOutputPort)
            return;

        if (!TryComp<ApcComponent>(ent, out var apc))
            return;

        var attemptEv = new ApcToggleMainBreakerAttemptEvent();
        RaiseLocalEvent(ent, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            LogSignal(ent, args.Trigger, "main breaker", "blocked");
            return;
        }

        _apc.ApcToggleBreaker(ent, apc);

        LogSignal(ent, args.Trigger, "main breaker", apc.MainBreakerEnabled);
    }

    private void OnBatteryMapInit(Entity<BatterySignalControlComponent> ent, ref MapInitEvent args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.ToggleInputPort, ent.Comp.ToggleOutputPort);
    }

    private void OnBatterySignalReceived(Entity<BatterySignalControlComponent> ent, ref SignalReceivedEvent args)
    {
        if (!TryComp<PowerNetworkBatteryComponent>(ent, out var battery))
            return;

        if (args.Port == ent.Comp.ToggleInputPort)
        {
            battery.CanCharge = !battery.CanCharge;
            LogSignal(ent, args.Trigger, "input breaker", battery.CanCharge);
        }
        else if (args.Port == ent.Comp.ToggleOutputPort)
        {
            battery.CanDischarge = !battery.CanDischarge;
            LogSignal(ent, args.Trigger, "output breaker", battery.CanDischarge);
        }
    }

    private void LogSignal(EntityUid uid, EntityUid? trigger, string breaker, bool enabled)
    {
        LogSignal(uid, trigger, breaker, enabled ? "enabled" : "disabled");
    }

    private void LogSignal(EntityUid uid, EntityUid? trigger, string breaker, string state)
    {
        _adminLogger.Add(LogType.DeviceLinking, LogImpact.Medium,
            $"{ToPrettyString(trigger):source} toggled the {breaker:breaker} of {ToPrettyString(uid):entity} via signal, result: {state:state}.");
    }
}
