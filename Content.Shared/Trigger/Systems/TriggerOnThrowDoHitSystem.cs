using Content.Shared.Throwing;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerOnThrowDoHitSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnThrowDoHitComponent, ThrowDoHitEvent>(OnHit);
    }

    private void OnHit(Entity<TriggerOnThrowDoHitComponent> ent, ref ThrowDoHitEvent args)
    {
        Trigger.Trigger(ent.Owner, args.Target, ent.Comp.KeyOut);
    }
}
