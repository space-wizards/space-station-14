using Content.Server.DeviceLinking.Events;
using Content.Server.Power.Components;
using Content.Server.Wires;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Content.Shared.Wires;
using Robust.Shared.Player;

namespace Content.Server.Doors.Systems;

public sealed class AirlockSystem : SharedAirlockSystem
{
    [Dependency] private readonly WiresSystem _wiresSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockComponent, ComponentInit>(OnAirlockInit);
        SubscribeLocalEvent<AirlockComponent, SignalReceivedEvent>(OnSignalReceived);

        SubscribeLocalEvent<AirlockComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AirlockComponent, ActivateInWorldEvent>(OnActivate, before: new[] { typeof(DoorSystem) });
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
        if (args.Port == component.AutoClosePort)
        {
            component.AutoClose = false;
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

    private void OnActivate(EntityUid uid, AirlockComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (TryComp<WiresPanelComponent>(uid, out var panel) &&
            panel.Open &&
            TryComp<ActorComponent>(args.User, out var actor))
        {
            if (TryComp<WiresPanelSecurityComponent>(uid, out var wiresPanelSecurity) &&
                !wiresPanelSecurity.WiresAccessible)
                return;

            _wiresSystem.OpenUserInterface(uid, actor.PlayerSession);
            args.Handled = true;
            return;
        }

        if (component.KeepOpenIfClicked)
        {
            // Disable auto close
            component.AutoClose = false;
        }
    }
}
