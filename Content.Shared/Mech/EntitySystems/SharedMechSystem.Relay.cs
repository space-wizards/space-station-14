using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Serilog;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem
{
    public void InitializeRelay()
    {
        SubscribeLocalEvent<MechComponent, GettingAttackedAttemptEvent>(RelayRefToPilot);
    }

    public void RelayToPilot<T>(EntityUid uid, MechComponent component, T args)
    {
        if (component.PilotSlot.ContainedEntity is not { } pilot)
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(pilot, ref ev);
    }

    public void RelayRefToPilot<T>(EntityUid uid, MechComponent component, ref T args)
    {
        if(component.PilotSlot.ContainedEntity is not { } pilot)
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(pilot, ref ev);

        args = ev.Args;
    }
}

[ByRefEvent]
public record struct MechPilotRelayedEvent<TEvent>(TEvent Args)
{
    public TEvent Args = Args;
}
