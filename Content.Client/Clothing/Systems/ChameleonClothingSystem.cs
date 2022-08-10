using System.Linq;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.Systems;

// All valid items for chameleon are calculated on client startup and stored in dictionary.
public sealed class ChameleonClothingSystem : SharedChameleonClothingSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;

    private static readonly SlotFlags[] IgnoredSlots =
    {
        SlotFlags.All,
        SlotFlags.PREVENTEQUIP,
        SlotFlags.NONE
    };
    private static readonly SlotFlags[] Slots = Enum.GetValues<SlotFlags>().Except(IgnoredSlots).ToArray();

    private readonly Dictionary<SlotFlags, List<string>> _data = new();

    public override void Initialize()
    {
        base.Initialize();
        PrepareAllVariants();
    }

    /// <summary>
    ///     Get a list of valid chameleon targets for this slots.
    /// </summary>
    public List<string> GetValidTargets(SlotFlags slot)
    {
        var list = new List<string>();
        foreach (var availableSlot in _data.Keys)
        {
            if (slot.HasFlag(availableSlot))
            {
                list.AddRange(_data[availableSlot]);
            }
        }

        // remove duplicates because some clothing can have multiple slots
        return list.Distinct().ToList();
    }

    private void PrepareAllVariants()
    {
        var prototypes = _proto.EnumeratePrototypes<EntityPrototype>();

        foreach (var proto in prototypes)
        {
            // check if this is valid clothing
            if (!IsValidTarget(proto))
                continue;
            if (!proto.TryGetComponent(out ClothingComponent? item, _factory))
                continue;

            // sort item by their slot flags
            // one item can be placed in several buckets
            foreach (var slot in Slots)
            {
                if (!item.Slots.HasFlag(slot))
                    continue;

                if (!_data.ContainsKey(slot))
                {
                    _data.Add(slot, new List<string>());
                }
                _data[slot].Add(proto.ID);
            }
        }
    }
}
