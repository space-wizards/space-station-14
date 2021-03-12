#nullable enable
using Content.Client.Animations;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Items;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Client.GameObjects.Components.Items
{
    [RegisterComponent]
    [ComponentReference(typeof(ISharedHandsComponent))]
    [ComponentReference(typeof(SharedHandsComponent))]
    public class HandsComponent : SharedHandsComponent
    {
        [Dependency] private readonly IGameHud _gameHud = default!;

        [ViewVariables]
        private HandsGui Gui { get; set; } = default!;

        /// <summary>
        ///     The name of the currently active hand.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private string? ActiveHand { get; set; }

        [ViewVariables]
        public IReadOnlyList<ClientHand> Hands => _hands;
        private readonly List<ClientHand> _hands = new();

        [ViewVariables]
        public IEntity? ActiveItem => TryGetActiveHand(out var hand) ? hand.HeldItem : null;

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

        private void OnHandClick(string handClicked)
        {
            if (!TryGetHand(handClicked, out var pressedHand))
                return;

            if (!TryGetActiveHand(out var activeHand))
                return;

            var pressedItem = pressedHand.HeldItem;
            var activeItem = activeHand.HeldItem;

            if (pressedItem == null && pressedHand != activeHand)
            {
                SendNetworkMessage(new ClientChangedHandMsg(pressedHand.Name)); //swap hand
                return;
            }

            if (handClicked == ActiveHand && activeItem != null)
            {
                SendNetworkMessage(new UseInHandMsg()); //use item in hand
                return;
            }

            if (handClicked != ActiveHand && pressedItem != null)
            {
                SendNetworkMessage(new ClientAttackByInHandMsg(pressedHand.Name)); //use active item on held item
                return;
            }
        }

        private void OnActivateInHand(string handActivated)
        {
            if (!TryGetHand(handActivated, out var activatedHand))
                return;

            SendNetworkMessage(new ActivateInHandMsg(activatedHand.Name));
        }

        private bool TryGetHand(string? handName, [NotNullWhen(true)] out ClientHand? foundHand)
        {
            foundHand = null;

            if (handName == null)
                return false;

            foreach (var hand in Hands)
            {
                if (hand.Name == handName)
                    foundHand = hand;
            }
            return foundHand != null;
        }

        private bool TryGetActiveHand([NotNullWhen(true)] out ClientHand? activeHand)
        {
            return TryGetHand(ActiveHand, out activeHand);
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

            RemoveHandLayers();
            _hands.Clear();

            ActiveHand = state.ActiveHand;
            foreach (var handState in state.Hands)
            {
                var newHand = new ClientHand(handState, GetHeldItem(handState.EntityUid), handState.Enabled);
                _hands.Add(newHand);
            }

            MakeHandLayers();
            SetGuiState();

            IEntity? GetHeldItem(EntityUid? uid)
            {
                IEntity? heldItem = null;
                if (uid != null)
                    Owner.EntityManager.TryGetEntity(uid.Value, out heldItem);

                return heldItem;
            }
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

        /// <summary>
        ///     Temporary hack for items to notify when they have changed their texture.
        /// </summary>
        public void RefreshInHands()
        {
            SetGuiState();
        }

        public override bool IsHolding(IEntity entity)
        {
            foreach (var hand in Hands)
            {
                if (hand.HeldItem == entity)
                    return true;
            }
            return false;
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

        private void RemoveHandLayers() //TODO: Replace with visualizer
        {
            if (_sprite == null)
                return;

            foreach (var hand in Hands)
            {
                var layerKey = GetHandLayerKey(hand.Name);
                var layer = _sprite.LayerMapGet(layerKey);
                _sprite.RemoveLayer(layer);
                _sprite.LayerMapRemove(layerKey);
            }

        }

        private void MakeHandLayers() //TODO: Replace with visualizer
        {
            if (_sprite == null)
                return;

            foreach (var hand in Hands)
            {
                var key = GetHandLayerKey(hand.Name);
                _sprite.LayerMapReserveBlank(key);

                var heldItem = hand.HeldItem;
                if (heldItem == null || !heldItem.TryGetComponent(out ItemComponent? item))
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
            }
        }

        private object GetHandLayerKey(string handName)
        {
            return $"hand-{handName}";
        }

        private void SetGuiState()
        {
            Gui.SetState(GetHandsGuiState());
        }

        private HandsGuiState GetHandsGuiState()
        {
            var handStates = new List<GuiHand>();

            foreach (var hand in _hands)
            {
                var handState = new GuiHand(hand.Name, hand.Location, hand.HeldItem, hand.Enabled);
                handStates.Add(handState);
            }
            return new HandsGuiState(handStates, ActiveHand);
        }
    }

    public class ClientHand
    {
        public string Name { get; }
        public HandLocation Location { get; }
        public IEntity? HeldItem { get; }
        public bool Enabled { get; }

        public ClientHand(SharedHand hand, IEntity? heldItem, bool enabled)
        {
            Name = hand.Name;
            Location = hand.Location;
            HeldItem = heldItem;
            Enabled = enabled;
        }
    }
}
