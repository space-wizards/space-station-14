using System.Collections.Generic;
using Content.Server.GameObjects;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.WorldState.States.Inventory
{
    [UsedImplicitly]
    public sealed class InventoryState : StateData<List<IEntity>>
    {
        public override string Name => "Inventory";

        public override List<IEntity> GetValue()
        {
            var inventory = new List<IEntity>();

            if (Owner.TryGetComponent(out HandsComponent handsComponent))
            {
                foreach (var item in handsComponent.GetAllHeldItems())
                {
                    inventory.Add(item.Owner);
                }
            }

            // TODO: InventoryComponent (Pockets were throwing)

            return inventory;
        }
    }
}
