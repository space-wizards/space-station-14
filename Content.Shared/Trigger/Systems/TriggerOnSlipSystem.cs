using Content.Shared.Slippery;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerOnSlipSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnSlipComponent, SlipEvent>(OnSlip);
    }

    private void OnSlip(Entity<TriggerOnSlipComponent> ent, ref SlipEvent args)
    {
        _trigger.Trigger(ent.Owner, args.Slipped, ent.Comp.KeyOut);
    }
}
