using Content.Shared.Stunnable;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class KnockdownOnTriggerSystem : XOnTriggerSystem<KnockdownOnTriggerComponent>
{
    [Dependency] private SharedStunSystem _stun = default!;

    protected override void OnTrigger(Entity<KnockdownOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        args.Handled |= _stun.TryKnockdown(
                        target,
                        ent.Comp.KnockdownAmount,
                        ent.Comp.Refresh,
                        ent.Comp.AutoStand,
                        ent.Comp.Drop,
                        true
                        );
    }
}
