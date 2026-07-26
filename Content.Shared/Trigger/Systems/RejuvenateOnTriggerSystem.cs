using Content.Shared.Administration.Systems;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class RejuvenateOnTriggerSystem : XOnTriggerSystem<RejuvenateOnTriggerComponent>
{
    [Dependency] private RejuvenateSystem _rejuvenate = default!;

    protected override void OnTrigger(Entity<RejuvenateOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _rejuvenate.PerformRejuvenate(target);
        args.Handled = true;
    }
}
