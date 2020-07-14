using System.Collections.Generic;
using Content.Server.GameObjects;
using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Hands
{
    [UsedImplicitly]
    public sealed class FreeHands : StateData<List<string>>
    {
        public override string Name => "FreeHands";

        public override List<string> GetValue()
        {
            var result = new List<string>();

            if (!Owner.TryGetComponent(out HandsComponent handsComponent))
            {
                return result;
            }

            foreach (var hand in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetHand(hand) == null)
                {
                    result.Add(hand);
                }
            }

            return result;
        }
    }
}
