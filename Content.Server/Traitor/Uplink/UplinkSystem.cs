using System.Linq;
using Content.Server.Store.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Server.Store.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Random;

namespace Content.Server.Traitor.Uplink
{
    public sealed class UplinkSystem : EntitySystem
    {
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly StoreSystem _store = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        [ValidatePrototypeId<CurrencyPrototype>]
        public const string TelecrystalCurrencyPrototype = "Telecrystal";

        /// <summary>
        ///     Gets the amount of TC on an "uplink"
        ///     Mostly just here for legacy systems based on uplink.
        /// </summary>
        /// <param name="component"></param>
        /// <returns>the amount of TC</returns>
        public int GetTCBalance(StoreComponent component)
        {
            FixedPoint2? tcBalance = component.Balance.GetValueOrDefault(TelecrystalCurrencyPrototype);
            return tcBalance?.Int() ?? 0;
        }

        /// <summary>
        /// Adds an uplink to the target
        /// </summary>
        /// <param name="user">The person who is getting the uplink</param>
        /// <param name="balance">The amount of currency on the uplink. If null, will just use the amount specified in the preset.</param>
        /// <param name="uplinkPresetId">The id of the storepreset</param>
        /// <param name="uplinkEntity">The entity that will actually have the uplink functionality. Defaults to the PDA if null.</param>
        /// <param name="giveDiscounts">Marker that enables discounts for uplink items.</param>
        /// <returns>Whether or not the uplink was added successfully</returns>
        public bool AddUplink(
            EntityUid user,
            FixedPoint2? balance,
            string uplinkPresetId = "StorePresetUplink",
            EntityUid? uplinkEntity = null,
            bool giveDiscounts = false
        )
        {
            // Try to find target item if none passed
            uplinkEntity ??= FindUplinkTarget(user);
            if (uplinkEntity == null)
            {
                return false;
            }

            var store = EnsureComp<StoreComponent>(uplinkEntity.Value);
            _store.InitializeFromPreset(uplinkPresetId, uplinkEntity.Value, store);
            var availableListings = _store.GetAvailableListings(user, store.Listings, store.Categories, null);
            store.Discounts = giveDiscounts
                ? InitializeDiscounts(availableListings, new DiscountSettings())
                : new List<StoreDiscountData>(0);
            store.AccountOwner = user;
            store.Balance.Clear();

            if (balance != null)
            {
                store.Balance.Clear();
                _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { TelecrystalCurrencyPrototype, balance.Value } }, uplinkEntity.Value, store);
            }

            // TODO add BUI. Currently can't be done outside of yaml -_-

            return true;
        }

        private List<StoreDiscountData> InitializeDiscounts(IEnumerable<ListingData> storeComponent, DiscountSettings settings)
        {
            var listingsByDiscountCategory = storeComponent.Where(x => x.DiscountOptions?.Count > 0)
                                                           .GroupBy(x => x.DiscountCategory)
                                                           .ToDictionary(
                                                               x => x.Key,
                                                               x => x.ToArray()
                                                           );
            var chosenDiscounts = new Dictionary<DiscountCategory, int>
            {
                [DiscountCategory.RareDiscounts] = 0,
                [DiscountCategory.UsualDiscounts] = 0,
                [DiscountCategory.VeryRareDiscounts] = 0,
            };
            var category2DiscountCount = 0;
            var category0DiscountCount = 0;
            for (var i = 0; i < settings.TotalAvailableDiscounts; i++)
            {
                var roll = _random.Next(100);

                switch (roll)
                {
                    case <= 2:
                        chosenDiscounts[DiscountCategory.VeryRareDiscounts]++;
                        if (category2DiscountCount >= settings.MaxCategory2Discounts)
                        {
                            chosenDiscounts[DiscountCategory.UsualDiscounts]++;
                        }
                        else
                        {
                            category2DiscountCount++;
                        }

                        break;
                    case <= 20:
                        if (category0DiscountCount <= settings.MaxCategory0Discounts)
                        {
                            chosenDiscounts[DiscountCategory.RareDiscounts]++;
                            category0DiscountCount++;
                        }
                        else
                        {
                            chosenDiscounts[DiscountCategory.UsualDiscounts]++;
                        }

                        break;
                    default:
                        chosenDiscounts[DiscountCategory.UsualDiscounts]++;
                        break;
                }
            }

            var list = new List<StoreDiscountData>();
            foreach (var (discountCategory, itemsCount) in chosenDiscounts)
            {
                if (itemsCount == 0)
                {
                    continue;
                }

                if (!listingsByDiscountCategory.TryGetValue(discountCategory, out var itemsForDiscount))
                {
                    continue;
                }

                var chosen = _random.GetItems(itemsForDiscount, itemsCount, allowDuplicates: false);
                var discountData = from listingData in chosen
                    let discount = _random.Pick(listingData.DiscountOptions!)
                    select new StoreDiscountData
                    {
                        ListingId = listingData.ID,
                        Count = 1,
                        DiscountByCurrency = new Dictionary<string, float>
                        {
                            [TelecrystalCurrencyPrototype] = discount
                        }
                    };
                list.AddRange(discountData);
            }

            return list;
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

    /// <summary>
    /// Settings for discount initializations.
    /// </summary>
    public sealed class DiscountSettings
    {
        /// <summary>
        /// Total count of discounts that can be attached to uplink.
        /// </summary>
        public int TotalAvailableDiscounts { get; set; } = 3;

        /// <summary>
        /// Maximum count of category 2 (not cheap stuff) items to be discounted.
        /// </summary>
        public int MaxCategory2Discounts { get; set; } = 1;

        /// <summary>
        /// Maximum count of category 0 (very low-costing stuff) items to be discounted.
        /// </summary>
        public int MaxCategory0Discounts { get; set; } = 2;
    }
}
