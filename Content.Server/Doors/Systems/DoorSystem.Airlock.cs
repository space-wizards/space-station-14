using Content.Server.DeviceLinking.Events;
using Content.Server.Power.Components;
using Content.Server.Wires;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Wires;
using Robust.Shared.Player;

namespace Content.Server.Doors.Systems;

public sealed partial class DoorSystem
{
    [Dependency] private readonly WiresSystem _wiresSystem = default!;

    private void InitializeAirlock()
    {
        SubscribeLocalEvent<AirlockComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<AirlockComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AirlockComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnSignalReceived(Entity<AirlockComponent> airlock, ref SignalReceivedEvent args)
    {
        if (args.Port != airlock.Comp.AutoClosePort || !airlock.Comp.AutoClose)
            return;

        airlock.Comp.AutoClose = false;
        Dirty(airlock);
    }

    private void OnPowerChanged(Entity<AirlockComponent> airlock, ref PowerChangedEvent args)
    {
        airlock.Comp.Powered = args.Powered;
        Dirty(airlock);

        if (!TryComp(airlock, out DoorComponent? door))
            return;

        if (args.Powered)
            UpdateAutoClose(airlock, door: door);
        else if (door.State == DoorState.Open)
            DoorSystem.SetNextStateChange((airlock, door), null);
    }

    private void OnActivate(Entity<AirlockComponent> airlock, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (TryComp<WiresPanelComponent>(airlock, out var panel) &&
            panel.Open &&
            TryComp<ActorComponent>(args.User, out var actor))
        {
            if (TryComp<WiresPanelSecurityComponent>(airlock, out var wiresPanelSecurity) &&
                !wiresPanelSecurity.WiresAccessible)
                return;

            _wiresSystem.OpenUserInterface(airlock, actor.PlayerSession);
            args.Handled = true;

            return;
        }

        if (!airlock.Comp.KeepOpenIfClicked || !airlock.Comp.AutoClose)
            return;

        // Disable auto close
        airlock.Comp.AutoClose = false;
        Dirty(airlock);
    }
}
