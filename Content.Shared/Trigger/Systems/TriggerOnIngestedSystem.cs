using Content.Shared.Nutrition;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerOnIngestedSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnIngestedComponent, IngestedEvent>(OnIngested);
    }

    private void OnIngested(Entity<TriggerOnIngestedComponent> ent, ref IngestedEvent args)
    {
        // We do Target instead of User since Target is the entity actually eating, while User is the one feeding and will not always be the same.
        Trigger.Trigger(ent.Owner, args.Target, ent.Comp.KeyOut);
    }
}
