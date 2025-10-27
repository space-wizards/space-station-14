using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class UncuffOnTriggerSystem : XOnTriggerSystem<UncuffOnTriggerComponent>
{
    [Dependency] private readonly SharedCuffableSystem _cuffable = default!;

    protected override void OnTrigger(Entity<UncuffOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!TryComp<CuffableComponent>(target, out var cuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;

        _cuffable.Uncuff(target, args.User, cuffs.LastAddedCuffs);
        args.Handled = true;
    }
}
