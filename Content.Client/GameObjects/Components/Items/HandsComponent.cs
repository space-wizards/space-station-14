#nullable enable
using Content.Client.Animations;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Storage;
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
        public HandsGui? Gui { get; private set; }

        public override void OnRemove()
        {
            Gui?.Dispose();
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
            Gui?.SetState(GetHandsGuiState());
        }

        private void HandlePlayerAttachedMsg()
        {
            if (Gui == null)
            {
                Gui = new HandsGui();
                _gameHud.HandsContainer.AddChild(Gui);
                Gui.HandClick += args => OnHandClick(args.HandClicked);
                Gui.HandActivate += args => OnActivateInHand(args.HandUsed);
                UpdateHandsGuiState();
            }
            Gui.Visible = true;
        }

        private void HandlePlayerDetachedMsg()
        {
            if (Gui != null)
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

        private HeldItemsVisualState GetHeldItemVisualState()
        {
            var itemStates = new List<ItemVisualState>();
            foreach (var hand in ReadOnlyHands)
            {
                var heldEntity = hand.HeldEntity;
                if (heldEntity == null)
                    continue;

                if (!heldEntity.TryGetComponent(out SharedItemComponent? item) || item.RsiPath == null)
                    continue;

                var state = $"inhand-{hand.Location.ToString().ToLowerInvariant()}";

                var prefix = item.EquippedPrefix;

                if (prefix != null)
                    state = $"{prefix}-" + state;

                itemStates.Add(new ItemVisualState(item.RsiPath, state, item.Color));
            }
            return new HeldItemsVisualState(itemStates);
        }

        private void RunPickupAnimation(PickupAnimationMessage msg)
        {
            if (!Owner.EntityManager.TryGetEntity(msg.EntityUid, out var entity))
                return;

            ReusableAnimations.AnimateEntityPickup(entity, msg.InitialPosition, msg.PickupDirection);
        }
    }
}
