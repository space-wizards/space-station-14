using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Item.ItemToggle;

/// <summary>
/// Handles <see cref="ComponentTogglerComponent"/> component manipulation.
/// </summary>
public sealed class ComponentTogglerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComponentTogglerComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnToggled(Entity<ComponentTogglerComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
        {
            var target = ent.Comp.Parent ? Transform(ent).ParentUid : ent.Owner;

            if (TerminatingOrDeleted(target))
                return;

            ent.Comp.Target = target;

            EntityManager.AddComponents(target, ent.Comp.Components);
        }
        else
        {
            if (ent.Comp.Target == null)
                return;

            if (TerminatingOrDeleted(ent.Comp.Target.Value))
                return;

            EntityManager.RemoveComponents(ent.Comp.Target.Value, ent.Comp.RemoveComponents ?? ent.Comp.Components);
        }
    }
}
