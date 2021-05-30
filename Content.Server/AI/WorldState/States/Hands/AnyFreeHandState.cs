using Content.Server.GameObjects.Components.GUI;
using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Hands
{
    [UsedImplicitly]
    public class AnyFreeHandState : StateData<bool>
    {
        public override string Name => "AnyFreeHand";
        public override bool GetValue()
        {
            if (!Owner.TryGetComponent(out HandsComponent? handsComponent))
            {
                return false;
            }

            foreach (var hand in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetItem(hand) == null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
