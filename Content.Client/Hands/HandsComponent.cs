using System.Collections.Generic;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Client.Hands
{
    [RegisterComponent]
    [ComponentReference(typeof(ISharedHandsComponent))]
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

            UpdateHandContainers();
            UpdateHandVisualizer();
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new HandsModifiedMessage { Hands = this });
        }

        public override void HandsModified()
        {
            UpdateHandContainers();
            UpdateHandVisualizer();

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

        public void UpdateHandVisualizer()
        {
            if (Owner.TryGetComponent(out SharedAppearanceComponent? appearance))
                appearance.SetData(HandsVisuals.VisualState, GetHandsVisualState());
        }

        private HandsVisualState GetHandsVisualState()
        {
            var hands = new List<HandVisualState>();
            foreach (var hand in Hands)
            {
                if (hand.HeldEntity == null)
                    continue;

                if (!hand.HeldEntity.TryGetComponent(out SharedItemComponent? item) || item.RsiPath == null)
                    continue;

                var handState = new HandVisualState(item.RsiPath, item.EquippedPrefix, hand.Location, item.Color);
                hands.Add(handState);
            }
            return new(hands);
        }
    }
}
