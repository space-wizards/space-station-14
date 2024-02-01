using Content.Shared.Access.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.PDA;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using System.Collections.Frozen;
using System.Linq;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class SharedChameleonClothingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPdaSystem _pdaSystem = default!;

    private static readonly SlotFlags[] IgnoredSlots =
    {
        SlotFlags.All,
        SlotFlags.PREVENTEQUIP,
        SlotFlags.NONE
    };
    private static readonly SlotFlags[] Slots = Enum.GetValues<SlotFlags>().Except(IgnoredSlots).ToArray();

    private Dictionary<SlotFlags, List<EntityPrototype>> _data = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ChameleonClothingComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReloaded);

        PrepareAllVariants();
    }

    private void OnGotEquipped(EntityUid uid, ChameleonClothingComponent component, GotEquippedEvent args)
    {
        component.User = args.Equipee;
    }

    private void OnGotUnequipped(EntityUid uid, ChameleonClothingComponent component, GotUnequippedEvent args)
    {
        component.User = null;
    }

    private void OnProtoReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            PrepareAllVariants();
    }

    /// <summary>
    ///     Get a list of valid chameleon targets for these slots.
    /// </summary>
    public IEnumerable<EntProtoId> GetValidTargets(SlotFlags slot, EntityWhitelist? whitelist = null)
    {
        return GetValidVariants(slot, _factory, whitelist);
    }

    // Updates chameleon visuals and meta information.
    // This function is called on a server after user selected new outfit.
    // And after that on a client after state was updated.
    // This 100% makes sure that server and client have exactly same data.
    protected void UpdateVisuals(EntityUid uid, ChameleonClothingComponent component)
    {
        if (string.IsNullOrEmpty(component.Default) ||
            !_proto.TryIndex(component.Default, out EntityPrototype? proto))
            return;

        // world sprite icon
        UpdateSprite(uid, proto);

        // copy name and description, unless its an ID card
        if (!HasComp<IdCardComponent>(uid))
        {
            var meta = MetaData(uid);
            _metaData.SetEntityName(uid, proto.Name, meta);
            _metaData.SetEntityDescription(uid, proto.Description, meta);
        }

        // item sprite logic
        if (TryComp(uid, out ItemComponent? item) &&
            proto.TryGetComponent<ItemComponent>(out var otherItem, _factory))
        {
            _itemSystem.CopyVisuals(uid, otherItem, item);
        }

        // clothing sprite logic
        if (TryComp(uid, out ClothingComponent? clothing) &&
            proto.TryGetComponent<ClothingComponent>(out var otherClothing, _factory))
        {
            _clothingSystem.CopyVisuals(uid, otherClothing, clothing);
        }

        // pda sprite logic
        if (TryComp<PdaComponent>(uid, out var pdaComp) &&
           proto.TryGetComponent<PdaComponent>(out var otherPdaComp, _factory)
           && otherPdaComp.State != null)
        {
            _pdaSystem.UpdatePdaState(uid, pdaComp, otherPdaComp.State);
        }
    }

    protected virtual void UpdateSprite(EntityUid uid, EntityPrototype proto) { }

    /// <summary>
    ///     Check if this entity prototype is valid target for chameleon item.
    /// </summary>
    protected bool IsValidTarget(EntityPrototype proto, SlotFlags chameleonSlot = SlotFlags.NONE, EntityWhitelist? whitelist = null)
    {
        // check if entity is valid
        if (proto.Abstract || proto.HideSpawnMenu)
            return false;

        // check if it is marked as valid chameleon target
        if (whitelist != null && !whitelist.IsValid(proto, _factory))
            return false;

        // check if it's valid clothing
        if (!proto.TryGetComponent<ClothingComponent>(out var clothing))
            return false;

        if (!clothing.Slots.HasFlag(chameleonSlot))
            return false;

        return true;
    }

    private void PrepareAllVariants()
    {
        _data.Clear();

        foreach (var slot in Slots)
            _data.Add(slot, new());

        var prototypes = _proto.EnumeratePrototypes<EntityPrototype>();

        foreach (var proto in prototypes)
        {
            // check if this is valid clothing
            if (!IsValidTarget(proto))
                continue;

            if (!proto.TryGetComponent<ClothingComponent>(out var item, _factory))
                continue;

            RegisterVariant(proto, item.Slots);
        }
    }

    /// <summary>
    /// Register a clothing prototype against its valid slots
    /// </summary>
    /// <param name="clothingPrototype">The EntityPrototype of the clothing</param>
    /// <param name="slot">A bitflag array of the valid slots for this article of clothing</param>
    public void RegisterVariant(EntityPrototype clothingPrototype, SlotFlags slot)
    {
        foreach (var availableSlot in _data.Keys)
        {
            if (slot.HasFlag(availableSlot)) _data[availableSlot].Add(clothingPrototype);
        }
    }

    /// <summary>
    /// Get all valid prototypes for a specific chameleon item
    /// </summary>
    /// <param name="slot">Which slot does the chameleon item fit in</param>
    /// <param name="factory">Component factory</param>
    /// <param name="whitelist">Optional whitelist to match against each prototype</param>
    /// <returns></returns>
    public IEnumerable<EntProtoId> GetValidVariants(SlotFlags slot, IComponentFactory factory, EntityWhitelist? whitelist = null)
    {
        var outList = new List<EntProtoId>();

        if (!_data.TryGetValue(slot, out var variants))
            return outList;

        foreach (var clothingProto in variants)
        {
            if (whitelist == null)
            {
                outList.Add(clothingProto.ID);
            }
            else if (whitelist.IsValid(clothingProto, factory))
            {
                outList.Add(clothingProto.ID);
            }
        }

        return outList;
    }
}
