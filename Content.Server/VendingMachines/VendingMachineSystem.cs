using System.Linq;
using Content.Server.Items;
using Content.Server.PowerCell.Components;
using Content.Server.Stunnable.Components;
using Content.Server.Weapon.Melee;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Notification.Managers;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Content.Server.VendingMachines.Components;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.DragDrop;
using Content.Shared.VendingMachines;

namespace Content.Server.VendingMachines
{
    public class VendingMachineSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedVendingMachineRestockComponent, DragDropRequestEvent>(OnDropRestockCompOnVendingMachine);         
        }

        private void OnDropRestockCompOnVendingMachine(EntityUid eUI, SharedVendingMachineRestockComponent restockComp, DragDropRequestEvent args)
        {
            if (EntityManager.ComponentManager.TryGetComponent<VendingMachineComponent>(args.Target, out var vendingMachineComp))
            {
                vendingMachineComp.RestockInventory();
            }
        }
    }
}
