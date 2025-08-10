using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.DisplacementMap;
using Content.Client.Examine;
using Content.Client.Strip;
using Content.Client.Verbs.UI;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Hands.Systems
{
    [UsedImplicitly]
    public sealed class HandsSystem : SharedHandsSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IUserInterfaceManager _ui = default!;

        [Dependency] private readonly StrippableSystem _stripSys = default!;
        [Dependency] private readonly SpriteSystem _sprite = default!;
        [Dependency] private readonly ExamineSystem _examine = default!;
        [Dependency] private readonly DisplacementMapSystem _displacement = default!;

        public event Action<string?>? OnPlayerSetActiveHand;
        public event Action<Entity<HandsComponent>>? OnPlayerHandsAdded;
        public event Action? OnPlayerHandsRemoved;
        public event Action<string, EntityUid>? OnPlayerItemAdded;
        public event Action<string, EntityUid>? OnPlayerItemRemoved;
        public event Action<string>? OnPlayerHandBlocked;
        public event Action<string>? OnPlayerHandUnblocked;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandsComponent, LocalPlayerAttachedEvent>(HandlePlayerAttached);
            SubscribeLocalEvent<HandsComponent, LocalPlayerDetachedEvent>(HandlePlayerDetached);
            SubscribeLocalEvent<HandsComponent, ComponentStartup>(OnHandsStartup);
            SubscribeLocalEvent<HandsComponent, ComponentShutdown>(OnHandsShutdown);
            SubscribeLocalEvent<HandsComponent, ComponentHandleState>(HandleComponentState);
            SubscribeLocalEvent<HandsComponent, VisualsChangedEvent>(OnVisualsChanged);

            OnHandSetActive += OnHandActivated;
        }

        #region StateHandling
        private void HandleComponentState(Entity<HandsComponent> ent, ref ComponentHandleState args)
        {
            if (args.Current is not HandsComponentState state)
                return;

            var newHands = state.Hands.Keys.Except(ent.Comp.Hands.Keys); // hands that were added between states
            var oldHands = ent.Comp.Hands.Keys.Except(state.Hands.Keys); // hands that were removed between states

            foreach (var handId in oldHands)
            {
                RemoveHand(ent.AsNullable(), handId);
            }

            foreach (var handId in state.SortedHands.Intersect(newHands))
            {
                AddHand(ent.AsNullable(), handId, state.Hands[handId]);
            }
            ent.Comp.SortedHands = new (state.SortedHands);

            SetActiveHand(ent.AsNullable(), state.ActiveHandId);

            _stripSys.UpdateUi(ent);
        }
        #endregion

        public void ReloadHandButtons()
        {
            if (!TryGetPlayerHands(out var hands))
            {
                return;
            }

            OnPlayerHandsAdded?.Invoke(hands.Value);
        }

        public override void DoDrop(Entity<HandsComponent?> ent,
            string handId,
            bool doDropInteraction = true,
            bool log = true)
        {
            base.DoDrop(ent, handId, doDropInteraction, log);

            if (TryGetHeldItem(ent, handId, out var held) && TryComp(held, out SpriteComponent? sprite))
                sprite.RenderOrder = EntityManager.CurrentTick.Value;
        }

        public EntityUid? GetActiveHandEntity()
        {
            return TryGetPlayerHands(out var hands) ? GetActiveItem(hands.Value.AsNullable()) : null;
        }

        /// <summary>
        ///     Get the hands component of the local player
        /// </summary>
        public bool TryGetPlayerHands([NotNullWhen(true)] out Entity<HandsComponent>? hands)
        {
            var player = _playerManager.LocalEntity;
            hands = null;
            if (player == null || !TryComp<HandsComponent>(player.Value, out var handsComp))
                return false;

            hands = (player.Value, handsComp);
            return true;
        }

        /// <summary>
        ///     Called when a user clicked on their hands GUI
        /// </summary>
        public void UIHandClick(Entity<HandsComponent> ent, string handName)
        {
            var hands = ent.Comp;
            if (hands.ActiveHandId == null)
                return;

            var pressedEntity = GetHeldItem(ent.AsNullable(), handName);
            var activeEntity = GetActiveItem(ent.AsNullable());

            if (handName == hands.ActiveHandId && activeEntity != null)
            {
                // use item in hand
                // it will always be attack_self() in my heart.
                RaisePredictiveEvent(new RequestUseInHandEvent());
                return;
            }

            if (handName != hands.ActiveHandId && pressedEntity == null)
            {
                // change active hand
                RaisePredictiveEvent(new RequestSetHandEvent(handName));
                return;
            }

            if (handName != hands.ActiveHandId && pressedEntity != null && activeEntity != null)
            {
                // use active item on held item
                RaisePredictiveEvent(new RequestHandInteractUsingEvent(handName));
                return;
            }

            if (handName != hands.ActiveHandId && pressedEntity != null && activeEntity == null)
            {
                // move the item to the active hand
                RaisePredictiveEvent(new RequestMoveHandItemEvent(handName));
            }
        }

        /// <summary>
        ///     Called when a user clicks on the little "activation" icon in the hands GUI. This is currently only used
        ///     by storage (backpacks, etc).
        /// </summary>
        public void UIHandActivate(string handName)
        {
            RaisePredictiveEvent(new RequestActivateInHandEvent(handName));
        }

        public void UIInventoryExamine(string handName)
        {
            if (!TryGetPlayerHands(out var hands) ||
                !TryGetHeldItem(hands.Value.AsNullable(), handName, out var heldEntity))
            {
                return;
            }

            _examine.DoExamine(heldEntity.Value);
        }

        /// <summary>
        ///     Called when a user clicks on the little "activation" icon in the hands GUI. This is currently only used
        ///     by storage (backpacks, etc).
        /// </summary>
        public void UIHandOpenContextMenu(string handName)
        {
            if (!TryGetPlayerHands(out var hands) ||
                !TryGetHeldItem(hands.Value.AsNullable(), handName, out var heldEntity))
            {
                return;
            }

            _ui.GetUIController<VerbMenuUIController>().OpenVerbMenu(heldEntity.Value);
        }

        public void UIHandAltActivateItem(string handName)
        {
            RaisePredictiveEvent(new RequestHandAltInteractEvent(handName));
        }

        #region visuals

        protected override void HandleEntityInserted(EntityUid uid, HandsComponent hands, EntInsertedIntoContainerMessage args)
        {
            base.HandleEntityInserted(uid, hands, args);

            if (!hands.Hands.ContainsKey(args.Container.ID))
                return;

            UpdateHandVisuals(uid, args.Entity, args.Container.ID);
            _stripSys.UpdateUi(uid);

            if (uid != _playerManager.LocalEntity)
                return;

            OnPlayerItemAdded?.Invoke(args.Container.ID, args.Entity);

            if (HasComp<VirtualItemComponent>(args.Entity))
                OnPlayerHandBlocked?.Invoke(args.Container.ID);
        }

        protected override void HandleEntityRemoved(EntityUid uid, HandsComponent hands, EntRemovedFromContainerMessage args)
        {
            base.HandleEntityRemoved(uid, hands, args);

            if (!hands.Hands.ContainsKey(args.Container.ID))
                return;

            UpdateHandVisuals(uid, args.Entity, args.Container.ID);
            _stripSys.UpdateUi(uid);

            if (uid != _playerManager.LocalEntity)
                return;

            OnPlayerItemRemoved?.Invoke(args.Container.ID, args.Entity);

            if (HasComp<VirtualItemComponent>(args.Entity))
                OnPlayerHandUnblocked?.Invoke(args.Container.ID);
        }

        /// <summary>
        ///     Update the players sprite with new in-hand visuals.
        /// </summary>
        private void UpdateHandVisuals(Entity<HandsComponent?, SpriteComponent?> ent, EntityUid held, string handId)
        {
            if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
                return;
            var handComp = ent.Comp1;
            var sprite = ent.Comp2;

            if (!TryGetHand((ent, handComp), handId, out var hand))
                return;

            // visual update might involve changes to the entity's effective sprite -> need to update hands GUI.
            if (ent == _playerManager.LocalEntity)
                OnPlayerItemAdded?.Invoke(handId, held);

            if (!handComp.ShowInHands)
                return;

            // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
            // may eventually bloat the player with lots of layers.
            if (handComp.RevealedLayers.TryGetValue(hand.Value.Location, out var revealedLayers))
            {
                foreach (var key in revealedLayers)
                {
                    _sprite.RemoveLayer((ent, sprite), key);
                }

                revealedLayers.Clear();
            }
            else
            {
                revealedLayers = new();
                handComp.RevealedLayers[hand.Value.Location] = revealedLayers;
            }

            if (HandIsEmpty((ent, handComp), handId))
            {
                // the held item was removed.
                RaiseLocalEvent(held, new HeldVisualsUpdatedEvent(ent, revealedLayers), true);
                return;
            }

            var ev = new GetInhandVisualsEvent(ent, hand.Value.Location);
            RaiseLocalEvent(held, ev);

            if (ev.Layers.Count == 0)
            {
                RaiseLocalEvent(held, new HeldVisualsUpdatedEvent(ent, revealedLayers), true);
                return;
            }

            // add the new layers
            foreach (var (key, layerData) in ev.Layers)
            {
                if (!revealedLayers.Add(key))
                {
                    Log.Warning($"Duplicate key for in-hand visuals: {key}. Are multiple components attempting to modify the same layer? Entity: {ToPrettyString(held)}");
                    continue;
                }

                var index = _sprite.LayerMapReserve((ent, sprite), key);

                // In case no RSI is given, use the item's base RSI as a default. This cuts down on a lot of unnecessary yaml entries.
                if (layerData.RsiPath == null
                    && layerData.TexturePath == null
                    && sprite[index].Rsi == null)
                {
                    if (TryComp<ItemComponent>(held, out var itemComponent) && itemComponent.RsiPath != null)
                        _sprite.LayerSetRsi((ent, sprite), index, new ResPath(itemComponent.RsiPath));
                    else if (TryComp(held, out SpriteComponent? clothingSprite))
                        _sprite.LayerSetRsi((ent, sprite), index, clothingSprite.BaseRSI);
                }

                _sprite.LayerSetData((ent, sprite), index, layerData);

                // Add displacement maps
                var displacement = hand.Value.Location switch
                {
                    HandLocation.Left => handComp.LeftHandDisplacement,
                    HandLocation.Right => handComp.RightHandDisplacement,
                    _ => handComp.HandDisplacement
                };

                if (displacement is not null && _displacement.TryAddDisplacement(displacement, (ent, sprite), index, key, out var displacementKey))
                    revealedLayers.Add(displacementKey);
            }

            RaiseLocalEvent(held, new HeldVisualsUpdatedEvent(ent, revealedLayers), true);
        }

        private void OnVisualsChanged(EntityUid uid, HandsComponent component, VisualsChangedEvent args)
        {
            // update hands visuals if this item is in a hand (rather then inventory or other container).
            if (!component.Hands.ContainsKey(args.ContainerId))
                return;
            UpdateHandVisuals((uid, component), GetEntity(args.Item), args.ContainerId);
        }
        #endregion

        #region Gui

        private void HandlePlayerAttached(EntityUid uid, HandsComponent component, LocalPlayerAttachedEvent args)
        {
            OnPlayerHandsAdded?.Invoke((uid, component));
        }

        private void HandlePlayerDetached(EntityUid uid, HandsComponent component, LocalPlayerDetachedEvent args)
        {
            OnPlayerHandsRemoved?.Invoke();
        }

        private void OnHandsStartup(EntityUid uid, HandsComponent component, ComponentStartup args)
        {
            if (_playerManager.LocalEntity == uid)
                OnPlayerHandsAdded?.Invoke((uid, component));
        }

        private void OnHandsShutdown(EntityUid uid, HandsComponent component, ComponentShutdown args)
        {
            if (_playerManager.LocalEntity == uid)
                OnPlayerHandsRemoved?.Invoke();
        }
        #endregion

        private void OnHandActivated(Entity<HandsComponent>? ent)
        {
            if (ent is not { } hand)
                return;

            if (_playerManager.LocalEntity != hand.Owner)
                return;

            OnPlayerSetActiveHand?.Invoke(hand.Comp.ActiveHandId);
        }
    }
}
