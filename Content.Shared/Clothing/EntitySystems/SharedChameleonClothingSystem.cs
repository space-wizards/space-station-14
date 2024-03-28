using Content.Shared.Access.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.PDA;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class SharedChameleonClothingSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] protected readonly IComponentFactory Factory = default!;

    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPdaSystem _pdaSystem = default!;

    /// <summary>
    /// Cache of all clothing prototypes sorted by their item slot
    /// </summary>
    private readonly Dictionary<SlotFlags, List<EntProtoId>> _data = new();

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
        return GetValidVariants(slot, Factory, whitelist);
    }

    // Updates chameleon visuals and meta information.
    // This function is called on a server after user selected new outfit.
    // And after that on a client after state was updated.
    // This 100% makes sure that server and client have exactly same data.
    protected virtual void UpdateVisuals(EntityUid uid, ChameleonClothingComponent component, EntityPrototype? proto = null)
    {
        if (!component.Default.HasValue)
            return;

        if (proto == null) proto = Proto.Index(component.Default.Value);

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
            proto.TryGetComponent<ItemComponent>(out var otherItem, Factory))
        {
            _itemSystem.CopyVisuals(uid, otherItem, item);
        }

        // clothing sprite logic
        if (TryComp(uid, out ClothingComponent? clothing) &&
            proto.TryGetComponent<ClothingComponent>(out var otherClothing, Factory))
        {
            _clothingSystem.CopyVisuals(uid, otherClothing, clothing);
        }

        // pda sprite logic
        if (TryComp<PdaComponent>(uid, out var pdaComp) &&
           proto.TryGetComponent<PdaComponent>(out var otherPdaComp, Factory)
           && otherPdaComp.State != null)
        {
            _pdaSystem.UpdatePdaState(uid, pdaComp, otherPdaComp.State);
        }
    }

    protected virtual void UpdateSprite(EntityUid uid, EntityPrototype proto) { }

    /// <summary>
    /// Check if this prototype is a valid chameleon target
    /// </summary>
    /// <param name="proto">The entity prototype to check</param>
    /// <param name="slot">The slot of the chameleon item that is trying to become this prototype, NONE for any slot</param>
    /// <param name="whitelist">The whitelist to test the prototype against</param>
    /// <returns>True if this is a valid chameleon target</returns>
    protected bool IsValidTarget(EntityPrototype proto, SlotFlags slot, EntityWhitelist? whitelist = null, ClothingComponent? clothingComp = null)
    {
        // check if entity is valid
        if (proto.Abstract || proto.HideSpawnMenu)
            return false;

        // check if it is marked as valid chameleon target
        if (whitelist != null && !whitelist.IsValid(proto, Factory))
            return false;

        //Check its a clothing item
        if (clothingComp == null && !proto.TryGetComponent(out clothingComp))
            return false;

        return IsValidSlot(proto, slot, clothingComp);
    }

    protected bool IsValidSlot(EntityPrototype proto, SlotFlags slot, ClothingComponent? clothingComp = null)
    {
        // Check its a clothing item
        if (clothingComp == null && !proto.TryGetComponent(out clothingComp))
            return false;

        return clothingComp.Slots.HasFlag(slot);
    }

    /// <summary>
    /// Enumerate all clothing items, and add them to a dictionary sorted by which item slots they fit in
    /// </summary>
    private void PrepareAllVariants()
    {
        SlotFlags[] ignoredSlots =
        {
            SlotFlags.All,
            SlotFlags.PREVENTEQUIP,
            SlotFlags.NONE,
            SlotFlags.BELT //no belt chameleons... yet
        };
        SlotFlags[] slots = Enum.GetValues<SlotFlags>().Except(ignoredSlots).ToArray();

        _data.Clear();
        foreach (var slot in slots)
            _data.Add(slot, new());

        var prototypes = Proto.EnumeratePrototypes<EntityPrototype>();

        foreach (var proto in prototypes)
        {
            if (!proto.TryGetComponent<ClothingComponent>(out var clothingComp, Factory))
                continue;

            //Check this item can be equipped to any slot, is not abstract and is not hidden from the spawn menu
            if (!IsValidTarget(proto, SlotFlags.NONE, clothingComp: clothingComp))
                continue;

            foreach (var availableSlot in _data.Keys)
            {
                if (IsValidSlot(proto, availableSlot, clothingComp: clothingComp))
                    _data[availableSlot].Add(proto.ID);
            }
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

        if (_data == null)
            return outList;

        if (!_data.TryGetValue(slot, out var variants))
            return outList;

        foreach (var clothingProto in variants)
        {
            if (whitelist == null)
            {
                outList.Add(clothingProto);
            }
            else
            {
                var proto = Proto.Index(clothingProto);
                if (whitelist.IsValid(proto, factory))
                    outList.Add(clothingProto);
            }
        }

        return outList;
    }
}
