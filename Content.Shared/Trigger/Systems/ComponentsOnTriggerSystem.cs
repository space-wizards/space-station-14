using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class ComponentsOnTriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddComponentsOnTriggerComponent, TriggerEvent>(HandleAddTrigger);
        SubscribeLocalEvent<RemoveComponentsOnTriggerComponent, TriggerEvent>(HandleRemoveTrigger);
        SubscribeLocalEvent<ToggleComponentsOnTriggerComponent, TriggerEvent>(HandleToggleTrigger);
    }

    private void HandleAddTrigger(Entity<AddComponentsOnTriggerComponent> ent, ref TriggerEvent args)
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

    private void HandleRemoveTrigger(Entity<RemoveComponentsOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (ent.Comp.TriggerOnce && ent.Comp.Triggered)
            return;

        EntityManager.RemoveComponents(target.Value, ent.Comp.Components);
        ent.Comp.Triggered = true;
        Dirty(ent);

        args.Handled = true;
    }

    private void HandleToggleTrigger(Entity<ToggleComponentsOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!ent.Comp.ComponentsAdded)
            EntityManager.AddComponents(target.Value, ent.Comp.Components, ent.Comp.RemoveExisting);
        else
            EntityManager.RemoveComponents(target.Value, ent.Comp.Components);

        ent.Comp.ComponentsAdded = !ent.Comp.ComponentsAdded;
        Dirty(ent);

        args.Handled = true;
    }
}
