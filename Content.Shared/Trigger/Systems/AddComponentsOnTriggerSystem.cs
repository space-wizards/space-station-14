using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class AddComponentsOnTriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddComponentsOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<AddComponentsOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (ent.Comp.TriggerOnce && ent.Comp.Triggered)
            return;

        EntityManager.AddComponents(target.Value, ent.Comp.Components, ent.Comp.RemoveExisting);
        ent.Comp.Triggered = true;
        Dirty(ent);

        args.Handled = true;
    }
}
