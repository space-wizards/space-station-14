using Content.Server.Store.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Server.Store.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.Traitor.Uplink
{
    public sealed class UplinkSystem : EntitySystem
    {
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly StoreSystem _store = default!;

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
            return tcBalance != null ? tcBalance.Value.Int() : 0;
        }

        /// <summary>
        /// Adds an uplink to the target
        /// </summary>
        /// <param name="user">The person who is getting the uplink</param>
        /// <param name="balance">The amount of currency on the uplink. If null, will just use the amount specified in the preset.</param>
        /// <param name="uplinkPresetId">The id of the storepreset</param>
        /// <param name="uplinkEntity">The entity that will actually have the uplink functionality. Defaults to the PDA if null.</param>
        /// <returns>Whether or not the uplink was added successfully</returns>
        public bool AddUplink(EntityUid user, FixedPoint2? balance, string uplinkPresetId = "StorePresetUplink", EntityUid? uplinkEntity = null)
        {
            // Try to find target item
            if (uplinkEntity == null)
            {
                uplinkEntity = FindUplinkTarget(user);
                if (uplinkEntity == null)
                    return false;
            }

            var store = EnsureComp<StoreComponent>(uplinkEntity.Value);
            _store.InitializeFromPreset(uplinkPresetId, store);
            store.AccountOwner = user;
            store.Balance.Clear();

            if (balance != null)
            {
                store.Balance.Clear();
                _store.TryAddCurrency(
                    new Dictionary<string, FixedPoint2>() { { TelecrystalCurrencyPrototype, balance.Value } }, store);
            }

            // TODO add BUI. Currently can't be done outside of yaml -_-

            return true;
        }

        private EntityUid? FindUplinkTarget(EntityUid user)
        {
            // Try to find PDA in inventory
            if (_inventorySystem.TryGetContainerSlotEnumerator(user, out var containerSlotEnumerator))
            {
                while (containerSlotEnumerator.MoveNext(out var pdaUid))
                {
                    if (!pdaUid.ContainedEntity.HasValue) continue;

                    if (HasComp<PDAComponent>(pdaUid.ContainedEntity.Value) || HasComp<StoreComponent>(pdaUid.ContainedEntity.Value))
                        return pdaUid.ContainedEntity.Value;
                }
            }

            // Also check hands
            foreach (var item in _handsSystem.EnumerateHeld(user))
            {
                if (HasComp<PDAComponent>(item) || HasComp<StoreComponent>(item))
                    return item;
            }

            return null;
        }
    }
}
