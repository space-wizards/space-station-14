using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Animations;
using Content.Client.HUD;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Client.Hands
{
    [RegisterComponent]
    [ComponentReference(typeof(ISharedHandsComponent))]
    [ComponentReference(typeof(SharedHandsComponent))]
    public class HandsComponent : SharedHandsComponent
    {
        [Dependency] private readonly IGameHud _gameHud = default!;

        private HandsGui? _gui;

        private readonly List<Hand> _hands = new();

        [ViewVariables] public IReadOnlyList<Hand> Hands => _hands;

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

        private void AddHand(Hand hand)
        {
            _sprite?.LayerMapReserveBlank($"hand-{hand.Name}");
            _hands.Insert(hand.Index, hand);
        }

        public Hand? GetHand(string? name)
        {
            return Hands.FirstOrDefault(hand => hand.Name == name);
        }

        private bool TryHand(string name, [NotNullWhen(true)] out Hand? hand)
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

        protected override void OnRemove()
        {
            base.OnRemove();

            _gui?.Dispose();
        }

        protected override void Initialize()
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
            {
                return;
            }

            var cast = (HandsComponentState) curState;
            foreach (var sharedHand in cast.Hands)
            {
                if (!TryHand(sharedHand.Name, out var hand))
                {
                    hand = new Hand(this, sharedHand, Owner.EntityManager);
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

            foreach (var currentHand in _hands.ToList())
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
        }

        private void HideHand(Hand hand)
        {
            _sprite?.LayerSetVisible($"hand-{hand.Name}", false);
        }

        private void UpdateHandSprites(Hand hand)
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

            if (!entity.TryGetComponent(out SharedItemComponent? item))
                return;

            if (item.RsiPath == null)
            {
                _sprite.LayerSetVisible($"hand-{name}", false);
            }
            else
            {
                var rsi = IoCManager.Resolve<IResourceCache>().GetResource<RSIResource>(SharedSpriteComponent.TextureRoot / item.RsiPath).RSI;

                var handName = hand.Location.ToString().ToLowerInvariant();
                var prefix = item.EquippedPrefix;
                var state = prefix != null ? $"{prefix}-inhand-{handName}" : $"inhand-{handName}";

                if (!rsi.TryGetState(state, out _))
                    return;

                var color = item.Color;

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
            base.Startup();
            ActiveIndex = _hands.LastOrDefault()?.Name;
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case HandEnabledMsg msg:
                {
                    var hand = GetHand(msg.Name);

                    if (hand?.Button == null)
                    {
                        break;
                    }

                    hand.Button.Blocked.Visible = false;

                    break;
                }
                case HandDisabledMsg msg:
                {
                    var hand = GetHand(msg.Name);

                    if (hand?.Button == null)
                    {
                        break;
                    }

                    hand.Button.Blocked.Visible = true;

                    break;
                }
            }
        }

        public void PlayerDetached() { _gui?.Parent?.RemoveChild(_gui); }

        public void PlayerAttached()
        {
            if (_gui == null)
            {
                _gui = new HandsGui();
            }
            else
            {
                _gui.Parent?.RemoveChild(_gui);
            }

            _gameHud.HandsContainer.AddChild(_gui);
            _gui.UpdateHandIcons();
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case AnimatePickupEntityMessage msg:
                {
                    if (Owner.EntityManager.TryGetEntity(msg.EntityId, out var entity))
                    {
                        ReusableAnimations.AnimateEntityPickup(entity, msg.EntityPosition, Owner.Transform.WorldPosition);
                    }
                    break;
                }
            }
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
            {
                return;
            }

            SendNetworkMessage(new ActivateInHandMsg(handIndex));
        }
    }

    public class Hand
    {
        private bool _enabled = true;

        public Hand(HandsComponent parent, SharedHand hand, IEntityManager manager, HandButton? button = null)
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
