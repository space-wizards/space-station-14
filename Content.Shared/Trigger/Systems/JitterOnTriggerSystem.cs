using Content.Shared.Jittering;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Network;

namespace Content.Shared.Trigger.Systems;

public sealed class JitterOnTriggerSystem : XOnTriggerSystem<JitterOnTriggerComponent>
{
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected override void OnTrigger(Entity<JitterOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // DoJitter mispredicts at the moment.
        // TODO: Fix this and remove the IsServer check.
        if (_net.IsServer)
            _jittering.DoJitter(target, ent.Comp.Time, ent.Comp.Refresh, ent.Comp.Amplitude, ent.Comp.Frequency, ent.Comp.ForceValueChange);
        args.Handled = true;
    }
}
