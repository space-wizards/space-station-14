using Content.Shared.Administration.Systems;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class RejuvenateOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RejuvenateOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<RejuvenateOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        _rejuvenate.PerformRejuvenate(target.Value);
        args.Handled = true;
    }
}
