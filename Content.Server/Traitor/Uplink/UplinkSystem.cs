using Content.Server.Store.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Server.Store.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Radio.Components;

namespace Content.Server.Traitor.Uplink
{
    public sealed class UplinkSystem : EntitySystem
    {
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly StoreSystem _store = default!;

        [ValidatePrototypeId<CurrencyPrototype>]
        public const string TelecrystalCurrencyPrototype = "Telecrystal";

        [ValidatePrototypeId<CurrencyPrototype>]
        public const string BluespaceCrystalCurrencyPrototype = "BluespaceCrystal";

        /// <summary>
        /// Adds an uplink to the target
        /// </summary>
        /// <param name="user">The person who is getting the uplink</param>
        /// <param name="balance">The amount of currency on the uplink. If null, will just use the amount specified in the preset.</param>
        /// <param name="uplinkPresetId">The id of the storepreset</param>
        /// <param name="uplinkEntity">The entity that will actually have the uplink functionality. Defaults to the PDA if null.</param>
        /// <returns>Whether or not the uplink was added successfully</returns>
        public bool AddUplink(EntityUid user, FixedPoint2? balance, EntityUid? uplinkEntity = null)
        {
            // Try to find target item
            if (uplinkEntity == null)
            {
                uplinkEntity = FindUplinkTarget(user);
                if (uplinkEntity == null)
                    return false;
            }

            EnsureComp<UplinkComponent>(uplinkEntity.Value);
            var store = EnsureComp<StoreComponent>(uplinkEntity.Value);
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

        public bool AddUplinkNT(EntityUid user, FixedPoint2? balance, EntityUid? uplinkEntity = null)
        {
            // Try to find target item
            if (uplinkEntity == null)
            {
                uplinkEntity = FindUplinkTargetNT(user);
                if (uplinkEntity == null)
                    return false;
            }

            EnsureComp<UplinkComponent>(uplinkEntity.Value);
            var store = EnsureComp<StoreComponent>(uplinkEntity.Value);
            store.AccountOwner = user;
            store.Balance.Clear();
            if (balance != null)
            {
                store.Balance.Clear();
                _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { BluespaceCrystalCurrencyPrototype, balance.Value } }, uplinkEntity.Value, store);
            }

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
                    if (!pdaUid.ContainedEntity.HasValue) continue;

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


        /// <summary>
        /// Finds the entity that can hold an uplink for an NT agent
        /// This checks the user's inventory slots for a headset that can hold an uplink. It also checks the user's hands.
        /// </summary>
         public EntityUid? FindUplinkTargetNT(EntityUid user)
        {
            // Try to find Headset in inventory
            if (_inventorySystem.TryGetContainerSlotEnumerator(user, out var containerSlotEnumerator))
            {
                while (containerSlotEnumerator.MoveNext(out var headsetUid))
                {
                    if (!headsetUid.ContainedEntity.HasValue) continue;

                    if (HasComp<HeadsetComponent>(headsetUid.ContainedEntity.Value) || HasComp<StoreComponent>(headsetUid.ContainedEntity.Value))
                        return headsetUid.ContainedEntity.Value;
                }
            }

            // Also check hands
            foreach (var item in _handsSystem.EnumerateHeld(user))
            {
                if (HasComp<HeadsetComponent>(item))
                    return item;
            }

            return null;
        }
    }
}
