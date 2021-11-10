using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Client.Hands
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandsComponent))]
    public class HandsComponent : SharedHandsComponent
    {
        public HandsGui? Gui { get; set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not HandsComponentState state)
                return;

            Hands.Clear();

            foreach (var handState in state.Hands)
            {
                var newHand = new Hand(handState.Name, handState.Location);
                Hands.Add(newHand);
            }

            ActiveHand = state.ActiveHand;

            HandsModified();
        }

        public override void HandsModified()
        {
            UpdateHandContainers();

            base.HandsModified();
        }

        public void UpdateHandContainers()
        {
            if (!Owner.TryGetComponent<ContainerManagerComponent>(out var containerMan))
                return;

            foreach (var hand in Hands)
            {
                if (hand.Container == null)
                {
                    containerMan.TryGetContainer(hand.Name, out var container);
                    hand.Container = container;
                }
            }
        }
    }
}
