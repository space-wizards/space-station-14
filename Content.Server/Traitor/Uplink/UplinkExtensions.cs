using Content.Server.Inventory.Components;
using Content.Server.PDA;
using Content.Server.PDA.Managers;
using Content.Server.Traitor.Uplink.Components;
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
            // Get target item
            if (uplinkEntity == null)
            {
                // Try to find PDA
                if (!user.TryGetComponent(out InventoryComponent? inventory))
                    return false;

                var foundPDA = inventory.LookupItems<PDAComponent>().FirstOrDefault();
                if (foundPDA == null)
                    return false;

                uplinkEntity = foundPDA.Owner;
            }

            var uplink = uplinkEntity.AddComponent<UplinkComponent>();
            uplink.UplinkAccount = account;

            return true;
        }
    }
}
