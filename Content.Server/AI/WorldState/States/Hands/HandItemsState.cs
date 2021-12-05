using System.Collections.Generic;
using Content.Server.Hands.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.WorldState.States.Hands
{
    [UsedImplicitly]
    public class HandItemsState : StateData<List<EntityUid>>
    {
        public override string Name => "HandItems";
        public override List<EntityUid> GetValue()
        {
            var result = new List<EntityUid>();
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out HandsComponent? handsComponent))
            {
                return result;
            }

            foreach (var hand in handsComponent.ActivePriorityEnumerable())
            {
                var item = handsComponent.GetItem(hand);

                if (item != null)
                {
                    result.Add(item.Owner);
                }
            }

            return result;
        }
    }
}
