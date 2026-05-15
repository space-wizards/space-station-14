using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Item.ItemToggle;

/// <summary>
/// On toggle handles the changes to ItemComponent.HeldPrefix. <see cref="ItemTogglePrefixComponent"/>.
/// </summary>
public sealed class ItemTogglePrefixSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemTogglePrefixComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnToggled(Entity<ItemTogglePrefixComponent> ent, ref ItemToggledEvent args)
    {
        _item.SetHeldPrefix(ent.Owner, args.Activated ? ent.Comp.PrefixOn : ent.Comp.PrefixOff);
    }
}
