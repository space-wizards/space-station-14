using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class SharedChameleonClothingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;

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
