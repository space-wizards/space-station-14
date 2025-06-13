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

    public void RelayToPilot<T>(Entity<MechComponent> uid, T args)
    {
        if (uid.Comp.PilotSlot.ContainedEntity is not { } pilot)
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(pilot, ref ev);
    }

    public void RelayRefToPilot<T>(Entity<MechComponent> uid, ref T args)
    {
        if (uid.Comp.PilotSlot.ContainedEntity is not { } pilot)
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
