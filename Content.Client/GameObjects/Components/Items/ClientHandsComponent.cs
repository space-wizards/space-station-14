// Only unused on .NET Core due to KeyValuePair.Deconstruct
// ReSharper disable once RedundantUsingDirective
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Interfaces.GameObjects;
using Content.Client.UserInterface;
using Content.Shared.GameObjects;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(IHandsComponent))]
    public class HandsComponent : SharedHandsComponent, IHandsComponent
    {
        private HandsGui _gui;

#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
#pragma warning restore 649

        [ViewVariables] private readonly Dictionary<string, IEntity> _hands = new Dictionary<string, IEntity>();

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
                    _setHand(slot, _hands[slot]);
                }
            }
        }

        public IEntity GetEntity(string index)
        {
            if (!string.IsNullOrEmpty(index) && _hands.TryGetValue(index, out var entity))
            {
                return entity;
            }

            return null;
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
                return;

            var cast = (HandsComponentState) curState;
            foreach (var (slot, uid) in cast.Hands)
            {
                IEntity entity = null;
                try
                {
                    entity = Owner.EntityManager.GetEntity(uid);
                }
                catch
                {
                    // Nothing.
                }

                _hands[slot] = entity;
                _setHand(slot, entity);
            }

            foreach (var slot in _hands.Keys.ToList())
            {
                if (!cast.Hands.ContainsKey(slot))
                {
                    _hands[slot] = null;
                    _setHand(slot, null);
                }
            }

            ActiveIndex = cast.ActiveIndex;

            _gui?.UpdateHandIcons();
            RefreshInHands();
        }

        private void _setHand(string hand, IEntity entity)
        {
            if (_sprite == null)
            {
                return;
            }

            if (entity == null)
            {
                _sprite.LayerSetVisible($"hand-{hand}", false);
                return;
            }

            SetInHands(hand, entity);
        }

        private void SetInHands(string hand, IEntity entity)
        {
            if (entity == null)
            {
                _sprite.LayerSetVisible($"hand-{hand}", false);

                return;
            }

            if (!entity.TryGetComponent(out ItemComponent item)) return;
            var maybeInhands = item.GetInHandStateInfo(hand);
            if (!maybeInhands.HasValue)
            {
                _sprite.LayerSetVisible($"hand-{hand}", false);
            }
            else
            {
                var (rsi, state) = maybeInhands.Value;
                _sprite.LayerSetVisible($"hand-{hand}", true);
                _sprite.LayerSetState($"hand-{hand}", state, rsi);
            }
        }

        public void RefreshInHands()
        {
            if (!Initialized) return;

            foreach (var (hand, entity) in _hands)
            {
                SetInHands(hand, entity);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "hands",
                new List<string>(),
                hands => hands.ForEach(slot => _hands.Add(slot, null)),
                () => _hands.Keys.ToList());

            serializer.DataField(this, x => ActiveIndex, "defaultHand", _hands.Keys.LastOrDefault());
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
                return;
            SendNetworkMessage(new ActivateInHandMsg(handIndex));
        }
    }
}
