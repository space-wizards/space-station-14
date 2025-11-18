using Content.Shared.Stunnable;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class StunOnTriggerSystem : XOnTriggerSystem<StunOnTriggerComponent>
{
    [Dependency] private readonly SharedStunSystem _stun = default!;

    protected override void OnTrigger(Entity<StunOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (ent.Comp.Refresh)
            _stun.TryUpdateStunDuration(target, ent.Comp.StunAmount);
        else
            _stun.TryAddStunDuration(target, ent.Comp.StunAmount);
    }
}
