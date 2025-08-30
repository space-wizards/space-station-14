using Content.Shared.Interaction.Events;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Mech.Components;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<MechComponent, GettingAttackedAttemptEvent>(RelayRefToPilot);
        SubscribeLocalEvent<MechComponent, AttemptPacifiedAttackEvent>(RelayRefToPilot);
    }

    private void RelayToPilot<T>(Entity<MechComponent> uid, T args) where T : class
    {
        if (!Vehicle.TryGetOperator(uid.Owner, out var operatorEnt))
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(operatorEnt.Value, ref ev);
    }

    private void RelayRefToPilot<T>(Entity<MechComponent> uid, ref T args) where T :struct
    {
        if (!Vehicle.TryGetOperator(uid.Owner, out var operatorEnt))
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(operatorEnt.Value, ref ev);

        args = ev.Args;
    }
}

[ByRefEvent]
public record struct MechPilotRelayedEvent<TEvent>(TEvent Args)
{
    public TEvent Args = Args;
}
