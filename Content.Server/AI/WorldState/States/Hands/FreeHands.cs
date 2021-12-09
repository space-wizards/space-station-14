using System.Collections.Generic;
using Content.Server.Hands.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.WorldState.States.Hands
{
    [UsedImplicitly]
    public sealed class FreeHands : StateData<List<string>>
    {
        public override string Name => "FreeHands";

        public override List<string> GetValue()
        {
            var result = new List<string>();

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out HandsComponent? handsComponent))
            {
                return result;
            }

            foreach (var hand in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetItem(hand) == null)
                {
                    result.Add(hand);
                }
            }

            return result;
        }
    }
}
