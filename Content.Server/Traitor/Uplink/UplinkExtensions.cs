using Content.Server.Hands.Components;
using Content.Server.Inventory.Components;
using Content.Server.PDA;
using Content.Server.PDA.Managers;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.Traitor.Uplink.Systems;
using Content.Shared.Inventory;
using Content.Shared.Traitor.Uplink;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.Linq;

namespace Content.Server.Traitor.Uplink
{
    public static class UplinkExtensions
    {
        public static bool AddUplink(IEntity user, UplinkAccount account, IEntity? uplinkEntity = null)
        {
            // Try to find target item
            if (uplinkEntity == null)
            {
                uplinkEntity = FindUplinkTarget(user);
                if (uplinkEntity == null)
                    return false;
            }

            var uplink = uplinkEntity.EnsureComponent<UplinkComponent>();
            uplinkEntity.EntityManager.EntitySysManager.GetEntitySystem<UplinkSystem>()
                .SetAccount(uplink, account);

            return true;
        }

        private static IEntity? FindUplinkTarget(IEntity user)
        {
            // Try to find PDA in inventory
            if (user.TryGetComponent(out InventoryComponent? inventory))
            {
                var foundPDA = inventory.LookupItems<PDAComponent>().FirstOrDefault();
                if (foundPDA != null)
                    return foundPDA.Owner;
            }

            // Also check hands
            if (user.TryGetComponent(out IHandsComponent? hands))
            {
                var heldItems = hands.GetAllHeldItems();
                foreach (var item in heldItems)
                {
                    if (item.Owner.HasComponent<PDAComponent>())
                        return item.Owner;
                }
            }

            return null;
        }
    }
}
