using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class SharedChameleonClothingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;

    protected virtual void UpdateVisuals(EntityUid uid, ChameleonClothingComponent component)
    {
        if (string.IsNullOrEmpty(component.SelectedId) ||
            !_proto.TryIndex(component.SelectedId, out EntityPrototype? proto))
            return;

        // copy name and description
        var meta = MetaData(uid);
        meta.EntityName = proto.Name;
        meta.EntityDescription = proto.Description;

        // item sprite logic
        if (TryComp(uid, out ItemComponent? item) &&
            proto.TryGetComponent(out ItemComponent? otherItem, _factory))
        {
            _itemSystem.CopyVisuals(uid, otherItem, item);
        }

        // clothing sprite logic
        if (TryComp(uid, out SharedClothingComponent? clothing) &&
            proto.TryGetComponent("Clothing", out SharedClothingComponent? otherClothing))
        {
            _clothingSystem.CopyVisuals(uid, otherClothing, clothing);
        }
    }

    /// <summary>
    ///     Check if this entity prototype is valid target for chameleon item.
    /// </summary>
    public bool IsValidTarget(EntityPrototype proto, SlotFlags chameleonSlot = SlotFlags.NONE)
    {
        // check if entity is valid
        if (proto.Abstract || proto.NoSpawn)
            return false;

        // check if it isn't marked as invalid chameleon target
        if (proto.TryGetComponent(out TagComponent? tags, _factory) && tags.Tags.Contains("IgnoreChameleon"))
            return false;

        // check if it's valid clothing
        if (!proto.TryGetComponent("Clothing", out SharedClothingComponent? clothing))
            return false;
        if (!clothing.Slots.HasFlag(chameleonSlot))
            return false;

        return true;
    }
}
