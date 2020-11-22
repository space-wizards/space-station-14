#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
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
        private string? _activeHand;

        private HandsGui? _gui;
        private readonly List<Hand> _hands = new List<Hand>();

        [ViewVariables] public IReadOnlyList<Hand> Hands => _hands;

        [ViewVariables] private ISpriteComponent? _sprite;

        [ViewVariables] public override IReadOnlyList<string> HandNames => _hands.Select(h => h.Name).ToList();

        [ViewVariables]
        public override string? ActiveHand
        {
            get => _activeHand;
            set => SetActiveHand(value, false);
        }

        [ViewVariables] public IEntity? HeldActiveEntity => GetEntity(ActiveHand);

        private void SetActiveHand(string? slotName, bool serverState)
        {
            if (_activeHand == slotName)
            {
                return;
            }

            var old = _activeHand;
            _activeHand = slotName;

            var interactionSystem = EntitySystem.Get<SharedInteractionSystem>();

            if (TryGetEntity(old, out var oldHeld))
            {
                interactionSystem.HandDeselectedInteraction(Owner, oldHeld);
            }

            if (TryGetEntity(_activeHand, out var newHeld))
            {
                interactionSystem.HandSelectedInteraction(Owner, newHeld);
            }

            if (!serverState)
            {
                SendNetworkMessage(new ClientChangedHandMsg(ActiveHand));
                Dirty();
            }
        }

        private Hand AddHand(SharedHand sharedHand)
        {
            var hand = new Hand(this, sharedHand, Owner.EntityManager);

            AddHand(hand);

            return hand;
        }

        private void AddHand(Hand hand)
        {
            _hands.Insert(hand.Index, hand);

            if (_activeHand == hand.Name && hand.Entity != null)
            {
                var interactionSystem = EntitySystem.Get<SharedInteractionSystem>();
                interactionSystem.HandSelectedInteraction(Owner, hand.Entity);
            }
        }

        private void RemoveHand(Hand hand)
        {
            _hands.Remove(hand);
            _gui?.RemoveHand(hand);
            HideHand(hand);

            if (_activeHand == hand.Name && hand.Entity != null)
            {
                var interactionSystem = EntitySystem.Get<SharedInteractionSystem>();
                interactionSystem.HandDeselectedInteraction(Owner, hand.Entity);
            }
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

        public bool TryGetEntity(string? handName, [NotNullWhen(true)] out IEntity? entity)
        {
            if (handName == null)
            {
                entity = null;
                return false;
            }

            return (entity = GetEntity(handName)) != null;
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
            if (!(curState is HandsComponentState state))
            {
                return;
            }

            foreach (var sharedHand in state.Hands)
            {
                if (!TryHand(sharedHand.Name, out var hand))
                {
                    hand = AddHand(sharedHand);
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
                if (state.Hands.All(newHand => newHand.Name != currentHand.Name))
                {
                    RemoveHand(currentHand);
                }
            }

            SetActiveHand(state.ActiveIndex, true);

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
            ActiveHand = _hands.LastOrDefault()?.Name;
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

        public void AttackByInHand(string index)
        {
            SendNetworkMessage(new ClientAttackByInHandMsg(index));
        }

        public void UseActiveHand()
        {
            if (GetEntity(ActiveHand) != null)
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
        private IEntity? _entity;

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

        public IEntity? Entity
        {
            get => _entity;
            set
            {
                if (_entity == value)
                {
                    return;
                }

                var old = _entity;
                _entity = value;

                if (Parent.ActiveHand != Name)
                {
                    return;
                }

                var interactionSystem = EntitySystem.Get<SharedInteractionSystem>();

                if (old != null)
                {
                    interactionSystem.HandDeselectedInteraction(Parent.Owner, old);
                }

                if (value != null)
                {
                    interactionSystem.HandSelectedInteraction(Parent.Owner, value);
                }
            }
        }

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
