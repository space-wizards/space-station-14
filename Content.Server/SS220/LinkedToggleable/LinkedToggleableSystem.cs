using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceNetwork;
using Content.Server.Power.Components;
using Content.Shared.SS220.LinkedToggleable;

namespace Content.Server.SS220.LinkedToggleable;

public sealed class LinkedToggleableSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LinkedToggleableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LinkedToggleableComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<LinkedToggleableComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private bool IsPowered(EntityUid uid, LinkedToggleableComponent component)
    {
        if (!component.RequirePower)
        {
            return true;
        }

        if (!TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver))
        {
            return false;
        }

        return powerReceiver.Powered;
    }

    private void OnPowerChanged(EntityUid uid, LinkedToggleableComponent component, ref PowerChangedEvent args)
    {
        UpdateState(uid, component, args.Powered);
    }

    private void UpdateState(EntityUid uid, LinkedToggleableComponent component, bool? power = null)
    {
        var oldState = component.State;

        power ??= IsPowered(uid, component);
        component.State = power.Value && component.Toggled;

        if (component.State != oldState)
            UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, LinkedToggleableComponent component)
    {
        _appearance.SetData(uid, LinkedToggleableVisuals.State, component.State);
    }

    private void OnSignalReceived(EntityUid uid, LinkedToggleableComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.OffPort)
            component.Toggled = false;
        else if (args.Port == component.OnPort)
            component.Toggled = true;
        else if (args.Port == component.TogglePort)
        {
            var state = SignalState.Momentary;
            args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

            if (state == SignalState.Momentary)
            {
                component.Toggled = !component.Toggled;
            }
            else
            {
                component.Toggled = state == SignalState.High;
            }
        }
        else
            return;

        UpdateState(uid, component);
    }

    private void OnInit(EntityUid uid, LinkedToggleableComponent component, ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(uid, component.OnPort, component.OffPort, component.TogglePort);
        UpdateState(uid, component, component.Toggled);
        UpdateAppearance(uid, component);
    }
}

