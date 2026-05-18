using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class AddComponentsOnTriggerSystem : XOnTriggerSystem<AddComponentsOnTriggerComponent>
{
    protected override void OnTrigger(Entity<AddComponentsOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (ent.Comp.TriggerOnce && ent.Comp.Triggered)
            return;

        EntityManager.AddComponents(target, ent.Comp.Components, ent.Comp.RemoveExisting);
        ent.Comp.Triggered = true;
        Dirty(ent);

        args.Handled = true;
    }
}

public sealed partial class RemoveComponentsOnTriggerSystem : XOnTriggerSystem<RemoveComponentsOnTriggerComponent>
{
    protected override void OnTrigger(Entity<RemoveComponentsOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (ent.Comp.TriggerOnce && ent.Comp.Triggered)
            return;

        EntityManager.RemoveComponents(target, ent.Comp.Components);
        ent.Comp.Triggered = true;
        Dirty(ent);

        args.Handled = true;
    }
}

public sealed partial class ToggleComponentsOnTriggerSystem : XOnTriggerSystem<ToggleComponentsOnTriggerComponent>
{
    protected override void OnTrigger(Entity<ToggleComponentsOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!ent.Comp.ComponentsAdded)
            EntityManager.AddComponents(target, ent.Comp.Components, ent.Comp.RemoveExisting);
        else
            EntityManager.RemoveComponents(target, ent.Comp.Components);

        ent.Comp.ComponentsAdded = !ent.Comp.ComponentsAdded;
        Dirty(ent);

        args.Handled = true;
    }
}
