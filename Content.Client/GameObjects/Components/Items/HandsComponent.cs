#nullable enable
using Content.Client.Animations;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Items;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Client.GameObjects.Components.Items
{
    [RegisterComponent]
    [ComponentReference(typeof(ISharedHandsComponent))]
    [ComponentReference(typeof(SharedHandsComponent))]
    public class HandsComponent : SharedHandsComponent
    {
        [Dependency] private readonly IGameHud _gameHud = default!;

        [ViewVariables]
        public HandsGui Gui { get; private set; } = default!;

        [ComponentDependency]
        private ISpriteComponent? _sprite = default!;

        public override void OnAdd()
        {
            base.OnAdd();
            Gui = new HandsGui();
            _gameHud.HandsContainer.AddChild(Gui);
        }

        public override void Initialize()
        {
            base.Initialize();
            Gui.HandClick += args => OnHandClick(args.HandClicked);
            Gui.HandActivate += args => OnActivateInHand(args.HandUsed);
        }

        public override void OnRemove()
        {
            Gui.Dispose();
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

            UpdateHandsSet();
            UpdateHandVisualizer();
            UpdateHandsGuiState();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg:
                    HandlePlayerAttachedMsg();
                    break;
                case PlayerDetachedMsg:
                    HandlePlayerDetachedMsg();
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case AnimatePickupEntityMessage msg:
                    HandleAnimatePickupEntityMessage(msg);
                    break;
            }
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
            if (!TryGetHand(handActivated, out var activatedHand))
                return;

            SendNetworkMessage(new ActivateInHandMsg(activatedHand.Name));
        }

        /// <summary>
        ///     Updates the containers of each hand, in case the hand of a container was not synced when the component state was received.
        /// </summary>
        public void UpdateHandsSet()
        {
            var containerMan = Owner.EnsureComponentWarn<ContainerManagerComponent>();
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
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                appearance.SetData(HeldItemsVisuals.VisualState, GetHeldItemVisualState());
        }

        public void UpdateHandsGuiState()
        {
            Gui.SetState(GetHandsGuiState());
        }

        private void HandleAnimatePickupEntityMessage(AnimatePickupEntityMessage msg)
        {
            if (!Owner.EntityManager.TryGetEntity(msg.EntityId, out var entity))
                return;

            ReusableAnimations.AnimateEntityPickup(entity, msg.EntityPosition, Owner.Transform.WorldPosition);
        }

        private void HandlePlayerAttachedMsg()
        {
            Gui.Visible = true;
        }

        private void HandlePlayerDetachedMsg()
        {
            Gui.Visible = false;
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

        private HeldItemsVisualState GetHeldItemVisualState() //TODO: update this to use new methods with updated ItemComponent
        {
            var itemStates = new List<ItemVisualState>();
            foreach (var hand in ReadOnlyHands)
            {
                var heldEntity = hand.HeldEntity;
                if (heldEntity == null)
                    continue;

                if (!heldEntity.TryGetComponent(out ItemComponent? item))
                    continue;

                var itemData = item.GetInHandStateInfo(hand.Location);
                if (itemData == null)
                    continue;

                var (rsi, state, color) = itemData.Value;

                itemStates.Add(new ItemVisualState(rsi, state.Name, color));
            }
            return new HeldItemsVisualState(itemStates);
        }
    }
}
