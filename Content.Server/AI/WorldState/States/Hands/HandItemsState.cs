using System.Collections.Generic;
using Content.Server.GameObjects;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.WorldState.States.Hands
{
    [UsedImplicitly]
    public class HandItemsState : StateData<List<IEntity>>
    {
        public override string Name => "HandItems";
        public override List<IEntity> GetValue()
        {
            var result = new List<IEntity>();
            if (!Owner.TryGetComponent(out HandsComponent handsComponent))
            {
                return result;
            }

            foreach (var hand in handsComponent.ActivePriorityEnumerable())
            {
                var item = handsComponent.GetHand(hand);

                if (item != null)
                {
                    result.Add(item.Owner);
                }
            }

            return result;
        }
    }
}
