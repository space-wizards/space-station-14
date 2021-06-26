using System.Collections.Generic;
using Content.Server.Hands.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.WorldState.States.Inventory
{
    [UsedImplicitly]
    public sealed class EnumerableInventoryState : StateData<IEnumerable<IEntity>>
    {
        public override string Name => "EnumerableInventory";

        public override IEnumerable<IEntity> GetValue()
        {
            if (Owner.TryGetComponent(out HandsComponent? handsComponent))
            {
                foreach (var item in handsComponent.GetAllHeldItems())
                {
                    if (item.Owner.Deleted)
                        continue;

                    yield return item.Owner;
                }
            }
        }
    }
}
