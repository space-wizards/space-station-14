#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Items;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Items
{
    [RegisterComponent]
    [ComponentReference(typeof(ISharedHandsComponent))]
    public class HandsComponent : SharedHandsComponent
    {
        [Dependency] private readonly IGameHud _gameHud = default!;

        private HandsGui? _gui;

        private readonly List<Hand> _hands = new();

        [ViewVariables] public IReadOnlyList<Hand> Hands => _hands;

        [ViewVariables] public string? ActiveIndex { get; private set; }

        [ViewVariables] private ISpriteComponent? _sprite;

        [ViewVariables] public IEntity? ActiveHand => GetEntity(ActiveIndex);

        private void AddHand(Hand hand)
        {
            _hands.Insert(hand.Index, hand);
        }

        public Hand? GetHand(string? name)
        {
            return Hands.FirstOrDefault(hand => hand.Name == name);
        }

        private bool TryHand(string name, [MaybeNullWhen(false)] out Hand hand)
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
                    break;
                case PlayerDetachedMsg _:
                    _gui?.Parent?.RemoveChild(_gui);
                    break;
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
