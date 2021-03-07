#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Animations;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Items;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Items
{
    [RegisterComponent]
    [ComponentReference(typeof(ISharedHandsComponent))]
    [ComponentReference(typeof(SharedHandsComponent))]
    public class HandsComponent : SharedHandsComponent
    {
        [Dependency] private readonly IGameHud _gameHud = default!;

        [ViewVariables]
        private HandsGui? _gui;

        [ViewVariables]
        private readonly List<ClientHand> _hands = new();

        [ViewVariables] public IReadOnlyList<ClientHand> Hands => _hands;

        [ViewVariables] public string? ActiveIndex { get; private set; }

        [ViewVariables] private ISpriteComponent? _sprite;

        [ViewVariables] public IEntity? ActiveHand => GetEntity(ActiveIndex);

        public override bool IsHolding(IEntity entity)
        {
            foreach (var hand in _hands)
            {
                if (hand.Entity == entity)
                {
                    return true;
                }
            }
            return false;
        }

        private void AddHand(ClientHand hand)
        {
            _sprite?.LayerMapReserveBlank($"hand-{hand.Name}");
            _hands.Insert(hand.Index, hand);
        }

        public ClientHand? GetHand(string? name)
        {
            return Hands.FirstOrDefault(hand => hand.Name == name);
        }

        private bool TryHand(string name, [NotNullWhen(true)] out ClientHand? hand)
        {
            return (hand = GetHand(name)) != null;
        }

        public IEntity? GetEntity(string? handName)
        {
            if (handName == null)
            {
                return null;
            }

            return GetHand(handName)?.Entity;
        }

        public override void OnRemove()
        {
            base.OnRemove();

            _gui?.Dispose();
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out _sprite))
            {
                foreach (var hand in _hands)
                {
                    _sprite.LayerMapReserveBlank($"hand-{hand.Name}");
                    UpdateHandSprites(hand);
                }
            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState == null)
                return;

            var cast = (HandsComponentState) curState;
            foreach (var sharedHand in cast.Hands)
            {
                if (!TryHand(sharedHand.Name, out var hand))
                {
                    hand = new ClientHand(this, sharedHand, Owner.EntityManager);
                    AddHand(hand);
                }
                else
                {
                    hand.Location = sharedHand.Location;

                    hand.Entity = sharedHand.EntityUid.HasValue
                        ? Owner.EntityManager.GetEntity(sharedHand.EntityUid.Value)
                        : null;
                }

                hand.Enabled = sharedHand.Enabled;

                UpdateHandSprites(hand);
            }

            foreach (var currentHand in _hands)
            {
                if (cast.Hands.All(newHand => newHand.Name != currentHand.Name))
                {
                    _hands.Remove(currentHand);
                    _gui?.RemoveHand(currentHand);
                    HideHand(currentHand);
                }
            }

            ActiveIndex = cast.ActiveIndex;

            _gui?.UpdateHandIcons();
            RefreshInHands();


            OnHandsModified(); //placeholder for auto-updating gui state
        }

        private void HideHand(ClientHand hand)
        {
            _sprite?.LayerSetVisible($"hand-{hand.Name}", false);
        }

        private void UpdateHandSprites(ClientHand hand)
        {
            if (_sprite == null)
            {
                return;
            }

            var entity = hand.Entity;
            var name = hand.Name;

            if (entity == null)
            {
                if (_sprite.LayerMapTryGet($"hand-{name}", out var layer))
                {
                    _sprite.LayerSetVisible(layer, false);
                }

                return;
            }

            if (!entity.TryGetComponent(out ItemComponent? item)) return;

            var maybeInHands = item.GetInHandStateInfo(hand.Location);

            if (!maybeInHands.HasValue)
            {
                _sprite.LayerSetVisible($"hand-{name}", false);
            }
            else
            {
                var (rsi, state, color) = maybeInHands.Value;
                _sprite.LayerSetColor($"hand-{name}", color);
                _sprite.LayerSetVisible($"hand-{name}", true);
                _sprite.LayerSetState($"hand-{name}", state, rsi);
            }
        }

        public void RefreshInHands()
        {
            if (!Initialized) return;

            foreach (var hand in _hands)
            {
                UpdateHandSprites(hand);
            }
        }

        protected override void Startup()
        {
            ActiveIndex = _hands.LastOrDefault()?.Name;
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

        private void HandleAnimatePickupEntityMessage(AnimatePickupEntityMessage msg)
        {
            if (Owner.EntityManager.TryGetEntity(msg.EntityId, out var entity))
            {
                ReusableAnimations.AnimateEntityPickup(entity, msg.EntityPosition, Owner.Transform.WorldPosition);
            }
        }

        private void HandlePlayerAttachedMsg()
        {
            if (_gui == null)
            {
                _gui = new HandsGui(this);
            }
            else
            {
                _gui.Parent?.RemoveChild(_gui);
            }
            _gameHud.HandsContainer.AddChild(_gui);
            _gui.UpdateHandIcons();
        }

        private void HandlePlayerDetachedMsg()
        {
            _gui?.Parent?.RemoveChild(_gui);
        }

        private void HandleHandEnabledMsg(HandEnabledMsg msg)
        {
            var hand = GetHand(msg.Name);

            if (hand?.Button == null)
                return;

            hand.Button.Blocked.Visible = false;
        }

        private void HandleHandDisabledMsg(HandDisabledMsg msg)
        {
            var hand = GetHand(msg.Name);

            if (hand?.Button == null)
                return;

            hand.Button.Blocked.Visible = true;
        }

        public void SendChangeHand(string index)
        {
            SendNetworkMessage(new ClientChangedHandMsg(index));
        }

        public void AttackByInHand(string index)
        {
            SendNetworkMessage(new ClientAttackByInHandMsg(index));
        }

        public void UseActiveHand()
        {
            if (GetEntity(ActiveIndex) != null)
            {
                SendNetworkMessage(new UseInHandMsg());
            }
        }

        public void ActivateItemInHand(string handIndex)
        {
            if (GetEntity(handIndex) == null)
                return;

            SendNetworkMessage(new ActivateInHandMsg(handIndex));
        }

        private void OnHandsModified() //TODO: Have methods call this when appropriate
        {
            SetGuiState();
        }

        private void SetGuiState()
        {
            _gui?.SetState(GetHandsGuiState());
        }

        private HandsGuiState GetHandsGuiState()
        {
            var handStates = new List<GuiHand>();

            foreach (var hand in _hands)
            {
                var handState = new GuiHand(hand.Name, hand.Location, hand.Entity);
                handStates.Add(handState);
            }
            return new HandsGuiState(handStates);
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
