using Content.Shared.Item;
using Content.Shared.Storage;

namespace Content.Shared.Nyanotrasen.Item.PseudoItem;

public partial class SharedPseudoItemSystem
{
    /// <summary>
    ///   Checks if the pseudo-item can be inserted into the specified storage entity.
    /// </summary>
    /// <remarks>
    ///   This function creates and uses a fake item component if the entity doesn't have one.
    /// </remarks>
    public bool CheckItemFits(Entity<PseudoItemComponent?> itemEnt, Entity<StorageComponent?> storageEnt)
    {
        if (!Resolve(itemEnt, ref itemEnt.Comp) || !Resolve(storageEnt, ref storageEnt.Comp))
            return false;

        if (!TryComp<MetaDataComponent>(itemEnt, out var metadata))
            return false;

        TryComp<ItemComponent>(itemEnt, out var item);
        // If the entity doesn't have an item comp, create a fake one
        // The fake component is never actually added to the entity
        item ??= new ItemComponent
        {
            Owner = itemEnt,
            Shape = itemEnt.Comp.Shape,
            Size = itemEnt.Comp.Size,
            StoredOffset = itemEnt.Comp.StoredOffset
        };

        return _storage.CanInsert(storageEnt, itemEnt, out _, storageEnt.Comp, item, ignoreStacks: true);
    }
}
