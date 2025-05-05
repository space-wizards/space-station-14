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
            ent.Comp.Target = target;
            EntityManager.AddComponents(target, ent.Comp.Components);
        }
        else
        {
            var target = ent.Comp.Target != null ? ent.Comp.Target : (ent.Comp.Parent ? Transform(ent).ParentUid : ent.Owner);
            EntityManager.RemoveComponents(target.Value, ent.Comp.RemoveComponents ?? ent.Comp.Components);
            ent.Comp.Target = null;
        }

        Dirty(ent);
    }
}
