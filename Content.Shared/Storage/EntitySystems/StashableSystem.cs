using Content.Shared.Item;
using Content.Shared.Storage.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// This handles obtaining effective sizes of the items for use with stashing said items.
/// </summary>
public sealed partial class StashableSystem : EntitySystem
{
    [Dependency] private SharedItemSystem _item = default!;

    /// <summary>
    /// Returns a size override for this item (if applicable) for use with SecretStashSystem.
    /// </summary>
    /// <param name="ent">entity which may or may not have a StashableComponent attached to it</param>
    /// <returns>The effective size of the item for use with stashing said item.</returns>
    public ItemSizePrototype GetStashableSizePrototype(Entity<StashableComponent?> ent)
    {
        // If we don't have a StashableComponent, we obtain the size provided by ItemComponent.
        if (!Resolve(ent.Owner, ref ent.Comp))
        {
            _item.TryGetItemSize(ent.Owner, out var defaultSize);
            return defaultSize;
        }

        // If the component specifies a size override to use, we will use that size override.
        if (ent.Comp.StashItemSizeOverride.HasValue)
        {
            return _item.GetSizePrototype(ent.Comp.StashItemSizeOverride.Value);
        }

        // Otherwise, obtain the item's base size, and return the next smallest size (if one exists)
        if (_item.TryGetItemSize(ent.Owner, out var baseSize))
        {
            return _item.GetSizeSmaller((ProtoId<ItemSizePrototype>) baseSize) ?? baseSize;
        }

        return baseSize; // fallback if things go horribly wrong
    }
}