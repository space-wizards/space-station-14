using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.StatusEffect;

namespace Content.Shared.Item.ItemToggle;

/// <summary>
/// Handles <see cref="ComponentTogglerComponent"/> component manipulation.
/// </summary>
public sealed class ComponentTogglerSystem : EntitySystem
{
    [Dependency] private readonly RefCountSystem _refCount = default!;

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

            _refCount.AddComponents(target, ent.Comp.Components);
        }
        else
        {
            if (ent.Comp.Target is not {} target)
                return;

            if (TerminatingOrDeleted(target))
                return;

            _refCount.RemoveComponents(target, ent.Comp.RemoveComponents ?? ent.Comp.Components);
        }
    }
}
