using System.Linq;
using Content.Server.Wires;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;

namespace Content.Server.Doors.Systems;

/// <inheritdoc/>
public sealed class TurnstileSystem : SharedTurnstileSystem
{
    [Dependency] private readonly WiresSystem _wires = default!;

    public override void SetSolenoidBypassed(Entity<TurnstileComponent> ent, bool value)
    {
        base.SetSolenoidBypassed(ent, value);

        // If the solenoid becomes un-bypassed, bolt the turnstile if the bolt wire is already cut.

        if (value)
            return;

        if (!TryComp<DoorBoltComponent>(ent, out var doorBolt))
            return;

        var boltCut = _wires.TryGetWires<DoorBoltWireAction>(ent).Any(w => w.IsCut);
        if (boltCut)
            Bolt.TrySetBoltsDown((ent, doorBolt), true);
    }
}
