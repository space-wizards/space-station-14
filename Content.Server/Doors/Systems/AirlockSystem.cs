using Content.Server.DeviceLinking.Events;
using Content.Server.Power.Components;
using Content.Server.Wires;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Wires;
using Robust.Shared.Player;

namespace Content.Server.Doors.Systems;

public sealed class AirlockSystem : SharedAirlockSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockComponent, ComponentInit>(OnAirlockInit);
        SubscribeLocalEvent<AirlockComponent, SignalReceivedEvent>(OnSignalReceived);

        SubscribeLocalEvent<AirlockComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnAirlockInit(EntityUid uid, AirlockComponent component, ComponentInit args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiverComponent))
        {
            Appearance.SetData(uid, DoorVisuals.Powered, receiverComponent.Powered);
        }
    }

    private void OnSignalReceived(EntityUid uid, AirlockComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.AutoClosePort && component.AutoClose)
        {
            component.AutoClose = false;
            Dirty(uid, component);
        }
    }

    private void OnPowerChanged(EntityUid uid, AirlockComponent component, ref PowerChangedEvent args)
    {
        component.Powered = args.Powered;
        Dirty(uid, component);

        if (TryComp<AppearanceComponent>(uid, out var appearanceComponent))
        {
            Appearance.SetData(uid, DoorVisuals.Powered, args.Powered, appearanceComponent);
        }

        if (!TryComp(uid, out DoorComponent? door))
            return;

        if (!args.Powered)
        {
            // stop any scheduled auto-closing
            if (door.State == DoorState.Open)
                DoorSystem.SetNextStateChange(uid, null);
        }
        else
        {
            UpdateAutoClose(uid, door: door);
        }
    }
}
