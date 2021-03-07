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
        private HandsGui? Gui { get; set; } //Should only have state sent to it

        [ViewVariables]
        public string? ActiveHand { get; private set; }

        [ViewVariables]
        public IReadOnlyList<ClientHand> Hands => _hands;
        private readonly List<ClientHand> _hands = new();

        [ViewVariables]
        private ISpriteComponent? _sprite;

        public override void Initialize()
        {
            base.Initialize();

            Gui = new HandsGui(this);
            _gameHud.HandsContainer.AddChild(Gui);
            Owner.TryGetComponent(out _sprite); //TODO: use component dependency?
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Gui?.Dispose();
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not HandsComponentState state)
                return;

            ActiveHand = state.ActiveIndex;

            foreach (var hand in _hands)
            {
                _sprite?.LayerMapRemove($"hand-{hand.Name}");
            }
            _hands.Clear();

            foreach (var handState in state.Hands)
            {
                var newHand = new ClientHand(this, handState, Owner.EntityManager);
                _hands.Add(newHand);
                _sprite?.LayerMapReserveBlank($"hand-{newHand.Name}");
            }
            OnHandsModified();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg _:
                    HandlePlayerAttachedMsg();
                    break;
                case PlayerDetachedMsg _:
                    HandlePlayerDetachedMsg();
                    break;
                case HandEnabledMsg msg:
                    HandleHandEnabledMsg(msg);
                    break;
                case HandDisabledMsg msg:
                    HandleHandDisabledMsg(msg);
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

        public override bool IsHolding(IEntity entity)
        {
            foreach (var hand in _hands)
            {
                if (hand.Entity == entity)
                    return true;
            }
            return false;
        }

        private void HandleAnimatePickupEntityMessage(AnimatePickupEntityMessage msg)
        {
            if (Owner.EntityManager.TryGetEntity(msg.EntityId, out var entity))
            {
                ReusableAnimations.AnimateEntityPickup(entity, msg.EntityPosition, Owner.Transform.WorldPosition);
            }
        }

        private void HandlePlayerAttachedMsg()
        {

        }

        private void HandlePlayerDetachedMsg()
        {

        }

        private void HandleHandEnabledMsg(HandEnabledMsg msg)
        {
        }

        private void HandleHandDisabledMsg(HandDisabledMsg msg)
        {
        }

        public void SendChangeHand(string index)
        {
        }

        public void AttackByInHand(string index)
        {
        }

        public void UseActiveHand()
        {
        }

        public void ActivateItemInHand(string handIndex)
        {
        }

        private void OnHandsModified() //TODO: Have methods call this when appropriate
        {
            SetGuiState();
        }

        private void SetGuiState()
        {
            Gui?.SetState(GetHandsGuiState());
        }

        private HandsGuiState GetHandsGuiState()
        {
            var handStates = new List<GuiHand>();

            foreach (var hand in _hands)
            {
                var handState = new GuiHand(hand.Name, hand.Location, hand.Entity);
                handStates.Add(handState);
            }
            return new HandsGuiState(handStates, ActiveHand);
        }
    }

    public class ClientHand
    {
        private bool _enabled = true;

        public ClientHand(HandsComponent parent, SharedHand hand, IEntityManager manager, HandButton? button = null)
        {
            Parent = parent;
            Index = hand.Index;
            Name = hand.Name;
            Location = hand.Location;
            Button = button;

            if (!hand.EntityUid.HasValue)
            {
                return;
            }

            manager.TryGetEntity(hand.EntityUid.Value, out var entity);
            Entity = entity;
        }

        private HandsComponent Parent { get; }
        public int Index { get; }
        public string Name { get; }
        public HandLocation Location { get; set; }
        public IEntity? Entity { get; set; }
        public HandButton? Button { get; set; }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;
                Parent.Dirty();

                var message = value
                    ? (ComponentMessage) new HandEnabledMsg(Name)
                    : new HandDisabledMsg(Name);

                Parent.HandleMessage(message, Parent);
                Parent.Owner.SendMessage(Parent, message);
            }
        }
    }
}
