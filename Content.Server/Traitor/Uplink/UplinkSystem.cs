using System.Linq;
using Content.Server.Store.Systems;
using Content.Server.StoreDiscount.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Traitor.Uplink;

public sealed class UplinkSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _subdermalImplant = default!;

    [ValidatePrototypeId<CurrencyPrototype>]
    public const string TelecrystalCurrencyPrototype = "Telecrystal";
    private const string FallbackUplinkImplant = "UplinkImplant";
    private const string FallbackUplinkCatalog = "UplinkUplinkImplanter";

    /// <summary>
    /// Adds an uplink to the target
    /// </summary>
    /// <param name="user">The person who is getting the uplink</param>
    /// <param name="balance">The amount of currency on the uplink. If null, will just use the amount specified in the preset.</param>
    /// <param name="uplinkEntity">The entity that will actually have the uplink functionality. Defaults to the PDA if null.</param>
    /// <param name="giveDiscounts">Marker that enables discounts for uplink items.</param>
    /// <returns>Whether or not the uplink was added successfully</returns>
    public bool AddUplink(
        EntityUid user,
        FixedPoint2 balance,
        EntityUid? uplinkEntity = null,
        bool giveDiscounts = false)
    {
        // Try to find target item if none passed

        uplinkEntity ??= FindUplinkTarget(user);

        if (uplinkEntity == null)
            return ImplantUplink(user, balance, giveDiscounts);

        EnsureComp<UplinkComponent>(uplinkEntity.Value);

        SetUplink(user, uplinkEntity.Value, balance, giveDiscounts);

        // TODO add BUI. Currently can't be done outside of yaml -_-
        // ^ What does this even mean?

        return true;
    }

    /// <summary>
    /// Configure TC for the uplink
    /// </summary>
    private void SetUplink(EntityUid user, EntityUid uplink, FixedPoint2 balance, bool giveDiscounts)
    {
        var store = EnsureComp<StoreComponent>(uplink);
        store.AccountOwner = user;

        store.Balance.Clear();
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { TelecrystalCurrencyPrototype, balance } },
            uplink,
            store);

        var uplinkInitializedEvent = new StoreInitializedEvent(
            TargetUser: user,
            Store: uplink,
            UseDiscounts: giveDiscounts,
            Listings: _store.GetAvailableListings(user, uplink, store)
                .ToArray());
        RaiseLocalEvent(ref uplinkInitializedEvent);
    }

    /// <summary>
    /// Implant an uplink as a fallback measure if the traitor had no PDA
    /// </summary>
    private bool ImplantUplink(EntityUid user, FixedPoint2 balance, bool giveDiscounts)
    {
        var implantProto = new string(FallbackUplinkImplant);

        if (!_proto.TryIndex<ListingPrototype>(FallbackUplinkCatalog, out var catalog))
            return false;

        if (!catalog.Cost.TryGetValue(TelecrystalCurrencyPrototype, out var cost))
            return false;

        if (balance < cost) // Can't use Math functions on FixedPoint2
            balance = 0;
        else
            balance = balance - cost;

        var implant = _subdermalImplant.AddImplant(user, implantProto);

        if (!HasComp<StoreComponent>(implant))
            return false;

        SetUplink(user, implant.Value, balance, giveDiscounts);
        return true;
    }

    /// <summary>
    /// Finds the entity that can hold an uplink for a user.
    /// Usually this is a pda in their pda slot, but can also be in their hands. (but not pockets or inside bag, etc.)
    /// </summary>
    public EntityUid? FindUplinkTarget(EntityUid user)
    {
        // Try to find PDA in inventory
        if (_inventorySystem.TryGetContainerSlotEnumerator(user, out var containerSlotEnumerator))
        {
            while (containerSlotEnumerator.MoveNext(out var pdaUid))
            {
                if (!pdaUid.ContainedEntity.HasValue)
                    continue;

                if (HasComp<PdaComponent>(pdaUid.ContainedEntity.Value) || HasComp<StoreComponent>(pdaUid.ContainedEntity.Value))
                    return pdaUid.ContainedEntity.Value;
            }
        }

        // Also check hands
        foreach (var item in _handsSystem.EnumerateHeld(user))
        {
            if (HasComp<PdaComponent>(item) || HasComp<StoreComponent>(item))
                return item;
        }

        return null;
    }
}
