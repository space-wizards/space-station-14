using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class UncuffOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedCuffableSystem _cuffable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UncuffOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<UncuffOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp<CuffableComponent>(target.Value, out var cuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;

        _cuffable.Uncuff(target.Value, args.User, cuffs.LastAddedCuffs);
        args.Handled = true;
    }
}
