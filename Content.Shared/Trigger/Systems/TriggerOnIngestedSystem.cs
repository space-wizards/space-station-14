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
        // args.Target is the entity being fed, while args.User is the entity doing the feeding.
        // Since they are not always equal (feeding someone by force, for example) we use a bool to decide which one is the trigger user.
        var user = ent.Comp.EatingIsUser ? args.Target : args.User;

        Trigger.Trigger(ent.Owner, user, ent.Comp.KeyOut);
    }
}
