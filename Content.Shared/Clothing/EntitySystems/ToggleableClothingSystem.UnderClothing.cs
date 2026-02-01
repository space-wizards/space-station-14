using Content.Shared.Clothing.Components;

namespace Content.Shared.Clothing.EntitySystems;

/// <summary>
/// Provides methods that store and re-equip clothing when toggleable clothing is put on or taken off.
/// Sidenote; god, I hate naming things.
/// </summary>
public sealed partial class ToggleableClothingSystem : EntitySystem
{
    /// <summary>
    ///     Tries to store clothing in <see cref="ToggleableClothingComponent.UnderClothingContainer"/>
    /// </summary>
    /// <param name="clothing">The clothing to be stored.</param>
    /// <param name="component">The ToggleableClothingComponent to store the clothing in.</param>
    /// <returns>True if clothing can be inserted and was inserted.</returns>
    private bool TryStoreUnderClothing(EntityUid clothing, ToggleableClothingComponent component)
    {
        if (component.UnderClothingContainer == null)
            return false;

        // There is already something in there? Either way, return false because we
        // expect one entity.
        if (component.UnderClothingContainer.ContainedEntity.HasValue)
            return false;

        return _containerSystem.Insert(clothing, component.UnderClothingContainer);
    }

    /// <summary>
    ///     Tries to equip any stored clothing kept in <see cref="ToggleableClothingComponent.UnderClothingContainer"/>.
    /// </summary>
    /// <param name="actor">The person wearing the ToggleableClothing.</param>
    /// <param name="component">The ToggleableClothingComponent to check for an stored items.</param>
    /// <returns>True if something was equipped OR if there is nothing to equip.</returns>
    private bool TryEquipUnderClothing(EntityUid actor, ToggleableClothingComponent component)
    {
        return TryEquipUnderClothing(actor, actor, component);
    }

    /// <summary>
    ///     Tries to equip any stored clothing kept in <see cref="ToggleableClothingComponent.UnderClothingContainer"/>.
    /// </summary>
    /// <param name="actor">The person trying to equip the clothing.</param>
    /// <param name="target">The person who to equip the clothing on.</param>
    /// <param name="component">The ToggleableClothingComponent to check for an stored items.</param>
    /// <returns>True if something was equipped OR if there is nothing to equip.</returns>
    private bool TryEquipUnderClothing(EntityUid actor, EntityUid target, ToggleableClothingComponent component)
    {
        // if there is no UnderClothingContainer, then why are we here?
        if (component.UnderClothingContainer == null)
            return true;

        // if nothing is contained so technically dropping nothing counts as a success
        if (!component.UnderClothingContainer.ContainedEntity.HasValue)
            return true;

        return _inventorySystem.TryEquip(actor, target, component.UnderClothingContainer.ContainedEntity.Value, component.Slot, force: true);
    }

    /// <summary>
    ///     Tries to equip any stored clothing kept in <see cref="ToggleableClothingComponent.UnderClothingContainer"/>.
    /// </summary>
    /// <param name="actor">The person trying to equip the clothing.</param>
    /// <param name="component">The AttachedClothing of the ToggleableClothing to check for an stored items.</param>
    /// <returns>True if something was equipped OR if there is nothing to equip.</returns>
    private bool TryEquipUnderClothing(EntityUid actor, AttachedClothingComponent component)
    {
        if (!TryComp<ToggleableClothingComponent>(component.AttachedUid, out var toggleableComp))
            return false;

        return TryEquipUnderClothing(actor, toggleableComp);
    }

    /// <summary>
    ///     Tries to drop any stored clothing kept in <see cref="ToggleableClothingComponent.UnderClothingContainer"/>.
    /// </summary>
    /// <param name="component">The ToggleableClothingComponent that is holding the item to be dropped.</param>
    /// <returns>True if there is not an item to be dropped OR it was successfully dropped.</returns>
    private bool TryDropUnderClothing(ToggleableClothingComponent component)
    {
        // if there is no UnderClothingContainer, then why are we here?
        if (component.UnderClothingContainer == null)
            return true;

        // if nothing is contained so technically dropping nothing counts as a success
        if (!component.UnderClothingContainer.ContainedEntity.HasValue)
            return true;

        return _containerSystem.TryRemoveFromContainer(component.UnderClothingContainer.ContainedEntity.Value);
    }
}
