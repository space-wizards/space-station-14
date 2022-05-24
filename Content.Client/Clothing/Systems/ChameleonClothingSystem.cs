using System.Linq;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.Systems;

public sealed class ChameleonClothingSystem : SharedChameleonClothingSystem
{
    private static readonly SlotFlags[] IgnoredSlots =
    {
        SlotFlags.All,
        SlotFlags.PREVENTEQUIP,
        SlotFlags.NONE
    };

    private static readonly SlotFlags[] Slots = Enum.GetValues<SlotFlags>().Except(IgnoredSlots).ToArray();

    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly Dictionary<SlotFlags, List<string>> _data = new();

    public override void Initialize()
    {
        base.Initialize();
        PrepareAllVariants();
    }

    public List<string> GetValidItems(SlotFlags slot)
    {
        var list = new List<string>();
        var availableSlots = _data.Keys;
        foreach (var availableSlot in availableSlots)
        {
            if (slot.HasFlag(availableSlot))
                list.AddRange(_data[availableSlot]);
        }

        return list;
    }

    private void PrepareAllVariants()
    {
        var prototypes = _proto.EnumeratePrototypes<EntityPrototype>();

        foreach (var proto in prototypes)
        {
            if (proto.Abstract || proto.NoSpawn)
                continue;

            if (!proto.TryGetComponent(out ClothingComponent? item))
                continue;

            foreach (var slot in Slots)
            {
                if (item.SlotFlags.HasFlag(slot))
                {
                    if (!_data.ContainsKey(slot))
                        _data.Add(slot, new List<string>());
                    _data[slot].Add(proto.ID);
                }
            }
        }
    }
}
