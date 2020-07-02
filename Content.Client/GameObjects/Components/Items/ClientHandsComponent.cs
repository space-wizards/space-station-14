// Only unused on .NET Core due to KeyValuePair.Deconstruct
// ReSharper disable once RedundantUsingDirective
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.Linq;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Items;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Items
{
    [RegisterComponent]
    public class HandsComponent : SharedHandsComponent
    {
        private HandsGui _gui;

#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
#pragma warning restore 649

        private readonly Dictionary<string, Hand> _hands = new Dictionary<string, Hand>();

        [ViewVariables] public IReadOnlyDictionary<string, Hand> Hands => _hands;

        [ViewVariables] public string ActiveIndex { get; private set; }

        [ViewVariables] private ISpriteComponent _sprite;

        [ViewVariables] public IEntity ActiveHand => GetEntity(ActiveIndex);

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
                foreach (var slot in _hands.Keys)
                {
                    _sprite.LayerMapReserveBlank($"hand-{slot}");

                    var hand = _hands[slot];
                    SetInHands(hand);
                }
            }
        }

        [CanBeNull]
        public IEntity GetEntity(string index)
        {
            if (!string.IsNullOrEmpty(index) && _hands.TryGetValue(index, out var hand))
            {
                return hand.Entity;
            }

            return null;
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
            {
                return;
            }

            var cast = (HandsComponentState) curState;
            foreach (var sharedHand in cast.Hands)
            {
                var hand = new Hand(sharedHand, Owner.EntityManager);
                _hands[sharedHand.Name] = hand;
                SetInHands(hand);
            }

            foreach (var slot in _hands.Keys.ToList())
            {
                if (cast.Hands.All(hand => hand.Name != slot))
                {
                    _hands[slot] = null;
                    HideHand(slot);
                }
            }

            ActiveIndex = cast.ActiveIndex;

            _gui?.UpdateHandIcons();
            RefreshInHands();
        }

        private void HideHand(string name)
        {
            _sprite.LayerSetVisible($"hand-{name}", false);
        }

        private void SetInHands(Hand hand)
        {
            if (_sprite == null)
            {
                return;
            }

            var entity = hand.Entity;
            var name = hand.Name;

            if (entity == null)
            {
                _sprite.LayerSetVisible($"hand-{name}", false);

                return;
            }

            if (!entity.TryGetComponent(out ItemComponent item)) return;
            var maybeInHands = item.GetInHandStateInfo(name);
            if (!maybeInHands.HasValue)
            {
                _sprite.LayerSetVisible($"hand-{name}", false);
            }
            else
            {
                var (rsi, state) = maybeInHands.Value;
                _sprite.LayerSetVisible($"hand-{name}", true);
                _sprite.LayerSetState($"hand-{name}", state, rsi);
            }
        }

        public void RefreshInHands()
        {
            if (!Initialized) return;

            foreach (var pair in _hands)
            {
                SetInHands(pair.Value);
            }
        }

        protected override void Startup()
        {
            ActiveIndex = _hands.Keys.LastOrDefault();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
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
                    _gui.Parent?.RemoveChild(_gui);
                    break;
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
        public readonly string Name;
        public readonly HandLocation Location;

        public Hand(SharedHand hand, IEntityManager manager)
        {
            Name = hand.Name;
            Location = hand.Location;

            if (!hand.EntityUid.HasValue)
            {
                return;
            }

            manager.TryGetEntity(hand.EntityUid.Value, out var entity);
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
