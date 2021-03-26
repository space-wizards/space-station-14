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
        ///     
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
            RemoveHandLayers();
            MakeHandLayers();
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

        private List<string> _handLayers = new(); //TODO: Replace with visualizer

        private void RemoveHandLayers() //TODO: Replace with visualizer
        {
            if (_sprite == null)
                return;

            foreach (var layerKey in _handLayers)
            {
                var layer = _sprite.LayerMapGet(layerKey);
                _sprite.RemoveLayer(layer);
                _sprite.LayerMapRemove(layerKey);
            }
            _handLayers.Clear();
        }

        private void MakeHandLayers() //TODO: Replace with visualizer
        {
            if (_sprite == null)
                return;

            foreach (var hand in ReadOnlyHands)
            {
                var key = $"hand-{hand.Name}";
                _sprite.LayerMapReserveBlank(key);

                var heldEntity = hand.HeldEntity;
                if (heldEntity == null || !heldEntity.TryGetComponent(out ItemComponent? item))
                    continue;

                var maybeInHands = item.GetInHandStateInfo(hand.Location);
                if (maybeInHands == null)
                    continue;

                var (rsi, state, color) = maybeInHands.Value;

                if (rsi == null)
                {
                    _sprite.LayerSetVisible(key, false);
                }
                else
                {
                    _sprite.LayerSetColor(key, color);
                    _sprite.LayerSetVisible(key, true);
                    _sprite.LayerSetState(key, state, rsi);
                }
                _handLayers.Add(key);
            }
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
    }
}
