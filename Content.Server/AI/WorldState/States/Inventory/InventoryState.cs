using System.Collections.Generic;
using Content.Server.Hands.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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
                    if ((!IoCManager.Resolve<IEntityManager>().EntityExists(item.Owner.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(item.Owner.Uid).EntityLifeStage) >= EntityLifeStage.Deleted)
                        continue;

                    yield return item.Owner;
                }
            }
        }
    }
}
