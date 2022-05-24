using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class SharedChameleonClothingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public bool IsValidTarget(EntityUid uid, string protoId, ChameleonClothingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!_proto.TryIndex(protoId, out EntityPrototype? proto))
            return false;

        return IsValidTarget(proto, component.Slot);
    }

    public bool IsValidTarget(EntityPrototype proto, SlotFlags slot = SlotFlags.NONE)
    {
        if (proto.Abstract || proto.NoSpawn)
            return false;
        if (!proto.TryGetComponent("Clothing", out SharedItemComponent? clothing))
            return false;
        if (!clothing.SlotFlags.HasFlag(slot))
            return false;

        return true;
    }
}
