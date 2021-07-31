using System.Collections.Generic;
using Content.Client.Animations;
using Content.Client.HUD;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Hands
{
    [RegisterComponent]
    [ComponentReference(typeof(ISharedHandsComponent))]
    [ComponentReference(typeof(SharedHandsComponent))]
    public class HandsComponent : SharedHandsComponent
    {
        [Dependency] private readonly IGameHud _gameHud = default!;

        [ViewVariables]
        public HandsGui? Gui { get; private set; }

        protected override void OnRemove()
        {
            ClearGui();
            base.OnRemove();
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not HandsComponentState state)
                return;

            Hands.Clear();

            foreach (var handState in state.Hands)
            {
                var newHand = new Hand(handState.Name, handState.Enabled, handState.Location);
                Hands.Add(newHand);
            }
            ActiveHand = state.ActiveHand;

            UpdateHandContainers();
            UpdateHandVisualizer();
            UpdateHandsGuiState();
        }

        public void SettupGui()
        {
            if (Gui == null)
            {
                Gui = new HandsGui();
                _gameHud.HandsContainer.AddChild(Gui);
                Gui.HandClick += args => OnHandClick(args.HandClicked);
                Gui.HandActivate += args => OnActivateInHand(args.HandUsed);
                UpdateHandsGuiState();
            }
        }

        public void ClearGui()
        {
            Gui?.Dispose();
            Gui = null;
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case PickupAnimationMessage msg:
                    RunPickupAnimation(msg);
                    break;
            }
        }

        public override void HandsModified()
        {
            base.HandsModified();

            UpdateHandContainers();
            UpdateHandVisualizer();
            UpdateHandsGuiState();
        }

        private void OnHandClick(string handClicked)
        {
            if (!TryGetHand(handClicked, out var pressedHand))
                return;

            if (!TryGetActiveHand(out var activeHand))
                return;

            var pressedEntity = pressedHand.HeldEntity;
            var activeEntity = activeHand.HeldEntity;

            if (pressedHand == activeHand && activeEntity != null)
            {
                SendNetworkMessage(new UseInHandMsg()); //use item in hand
                return;
            }

            if (pressedHand != activeHand && pressedEntity == null)
            {
                SendNetworkMessage(new ClientChangedHandMsg(pressedHand.Name)); //swap hand
                return;
            }

            if (pressedHand != activeHand && pressedEntity != null && activeEntity != null)
            {
                SendNetworkMessage(new ClientAttackByInHandMsg(pressedHand.Name)); //use active item on held item
                return;
            }

            if (pressedHand != activeHand && pressedEntity != null && activeEntity == null)
            {
                SendNetworkMessage(new MoveItemFromHandMsg(pressedHand.Name)); //move item in hand to active hand
                return;
            }
        }

        private void OnActivateInHand(string handActivated)
        {
            SendNetworkMessage(new ActivateInHandMsg(handActivated));
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

        public void UpdateHandsGuiState()
        {
            Gui?.SetState(GetHandsGuiState());
        }

        private HandsGuiState GetHandsGuiState()
        {
            var handStates = new List<GuiHand>();

            foreach (var hand in ReadOnlyHands)
            {
                var handState = new GuiHand(hand.Name, hand.Location, hand.HeldEntity, hand.Enabled);
                handStates.Add(handState);
            }
            return new HandsGuiState(handStates, ActiveHand);
        }

        private HandsVisualState GetHandsVisualState()
        {
            var hands = new List<HandVisualState>();
            foreach (var hand in ReadOnlyHands)
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

        private void RunPickupAnimation(PickupAnimationMessage msg)
        {
            if (!Owner.EntityManager.TryGetEntity(msg.EntityUid, out var entity))
                return;

            if (!IoCManager.Resolve<IGameTiming>().IsFirstTimePredicted)
                return;

            ReusableAnimations.AnimateEntityPickup(entity, msg.InitialPosition, msg.PickupDirection);
        }
    }
}
