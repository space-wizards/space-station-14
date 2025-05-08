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
        var target = ent.Comp.Parent ? Transform(ent).ParentUid : ent.Owner;
        if (TerminatingOrDeleted(target))
            return;

        if (args.Activated)
            EntityManager.AddComponents(target, ent.Comp.Components);
        else
            EntityManager.RemoveComponents(target, ent.Comp.RemoveComponents ?? ent.Comp.Components);
    }
}
