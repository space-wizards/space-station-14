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

        public int GetTCBalance(StoreComponent component)
        {
            FixedPoint2? tcBalance = component.Balance.GetValueOrDefault("Telecrystal");
            var emo = tcBalance != null ? tcBalance.Value.Int() : 0;

            return emo; //hey ma, that's me!
        }

        public bool AddUplink(EntityUid user, FixedPoint2 balance, string uplinkPresetId = "StorePresetUplink", EntityUid? uplinkEntity = null)
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

            var currency = new Dictionary<string, FixedPoint2>
            {
                { "Telecrystal", balance }
            };

            _store.TryAddCurrency(currency, store);

            if (!HasComp<PDAComponent>(uplinkEntity.Value))
                store.ActivateInHand = true;

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

                    if (HasComp<PDAComponent>(pdaUid.ContainedEntity.Value))
                        return pdaUid.ContainedEntity.Value;
                }
            }

            // Also check hands
            foreach (var item in _handsSystem.EnumerateHeld(user))
            {
                if (HasComp<PDAComponent>(item))
                    return item;
            }

            return null;
        }
    }
}
