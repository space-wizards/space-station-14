using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Animations;
using Content.Client.HUD;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Hands
{
    [UsedImplicitly]
    public sealed class HandsSystem : SharedHandsSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedHandsComponent, EntRemovedFromContainerMessage>(HandleContainerModified);
            SubscribeLocalEvent<SharedHandsComponent, EntInsertedIntoContainerMessage>(HandleContainerModified);

            SubscribeLocalEvent<HandsComponent, PlayerAttachedEvent>(HandlePlayerAttached);
            SubscribeLocalEvent<HandsComponent, PlayerDetachedEvent>(HandlePlayerDetached);
            SubscribeLocalEvent<HandsComponent, ComponentRemove>(HandleCompRemove);
            SubscribeLocalEvent<HandsComponent, ComponentHandleState>(HandleComponentState);
            SubscribeLocalEvent<HandsComponent, VisualsChangedEvent>(OnVisualsChanged);

            SubscribeNetworkEvent<PickupAnimationEvent>(HandlePickupAnimation);
        }

        #region StateHandling
        private void HandleComponentState(EntityUid uid, HandsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not HandsComponentState state)
                return;

            var handsModified = component.Hands.Count != state.Hands.Count;
            var manager = EnsureComp<ContainerManagerComponent>(uid);
            foreach (var hand in state.Hands)
            {
                if (component.Hands.TryAdd(hand.Name, hand))
                {
                    hand.Container = _containerSystem.EnsureContainer<ContainerSlot>(uid, hand.Name, manager);
                    handsModified = true;
                }
            }

            if (handsModified)
            {
                foreach (var name in component.Hands.Keys)
                {
                    if (!state.HandNames.Contains(name))
                        component.Hands.Remove(name);
                }

                component.SortedHands = new(state.HandNames);
            }

            TrySetActiveHand(uid, state.ActiveHand, component);

            if (uid == _playerManager.LocalPlayer?.ControlledEntity)
                UpdateGui();
        }
        #endregion

        #region PickupAnimation
        private void HandlePickupAnimation(PickupAnimationEvent msg)
        {
            PickupAnimation(msg.ItemUid, msg.InitialPosition, msg.FinalPosition);
        }

        public override void PickupAnimation(EntityUid item, EntityCoordinates initialPosition, Vector2 finalPosition,
            EntityUid? exclude)
        {
            PickupAnimation(item, initialPosition, finalPosition);
        }

        public void PickupAnimation(EntityUid item, EntityCoordinates initialPosition, Vector2 finalPosition)
        {
            if (!_gameTiming.IsFirstTimePredicted)
                return;

            if (finalPosition.EqualsApprox(initialPosition.Position, tolerance: 0.1f))
                return;

            ReusableAnimations.AnimateEntityPickup(item, initialPosition, finalPosition);
        }
        #endregion

        public EntityUid? GetActiveHandEntity()
        {
            return TryGetPlayerHands(out var hands) ? hands.ActiveHandEntity : null;
        }

        /// <summary>
        ///     Get the hands component of the local player
        /// </summary>
        public bool TryGetPlayerHands([NotNullWhen(true)] out HandsComponent? hands)
        {
            var player = _playerManager.LocalPlayer?.ControlledEntity;
            hands = null;
            return player != null && TryComp(player.Value, out hands);
        }

        /// <summary>
        ///     Called when a user clicked on their hands GUI
        /// </summary>
        public void UIHandClick(HandsComponent hands, string handName)
        {
            if (!hands.Hands.TryGetValue(handName, out var pressedHand))
                return;

            if (hands.ActiveHand == null)
                return;

            var pressedEntity = pressedHand.HeldEntity;
            var activeEntity = hands.ActiveHand.HeldEntity;

            if (pressedHand == hands.ActiveHand && activeEntity != null)
            {
                // use item in hand
                // it will always be attack_self() in my heart.
                EntityManager.RaisePredictiveEvent(new RequestUseInHandEvent());
                return;
            }

            if (pressedHand != hands.ActiveHand && pressedEntity == null)
            {
                // change active hand
                EntityManager.RaisePredictiveEvent(new RequestSetHandEvent(handName));
                return;
            }

            if (pressedHand != hands.ActiveHand && pressedEntity != null && activeEntity != null)
            {
                // use active item on held item
                EntityManager.RaisePredictiveEvent(new RequestHandInteractUsingEvent(pressedHand.Name));
                return;
            }

            if (pressedHand != hands.ActiveHand && pressedEntity != null && activeEntity == null)
            {
                // move the item to the active hand
                EntityManager.RaisePredictiveEvent(new RequestMoveHandItemEvent(pressedHand.Name));
            }
        }

        /// <summary>
        ///     Called when a user clicks on the little "activation" icon in the hands GUI. This is currently only used
        ///     by storage (backpacks, etc).
        /// </summary>
        public void UIHandActivate(string handName)
        {
            EntityManager.RaisePredictiveEvent(new RequestActivateInHandEvent(handName));
        }

        #region visuals
        private void HandleContainerModified(EntityUid uid, SharedHandsComponent handComp, ContainerModifiedMessage args)
        {
            if (handComp.Hands.TryGetValue(args.Container.ID, out var hand))
            {
                UpdateHandVisuals(uid, args.Entity, hand);
            }
        }

        /// <summary>
        ///     Update the players sprite with new in-hand visuals.
        /// </summary>
        private void UpdateHandVisuals(EntityUid uid, EntityUid held, Hand hand, HandsComponent? handComp = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(uid, ref handComp, ref sprite, false))
                return;

            if (uid == _playerManager.LocalPlayer?.ControlledEntity)
                UpdateGui();

            if (!handComp.ShowInHands)
                return;

            // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
            // may eventually bloat the player with lots of layers.
            if (handComp.RevealedLayers.TryGetValue(hand.Location, out var revealedLayers))
            {
                foreach (var key in revealedLayers)
                {
                    sprite.RemoveLayer(key);
                }
                revealedLayers.Clear();
            }
            else
            {
                revealedLayers = new();
                handComp.RevealedLayers[hand.Location] = revealedLayers;
            }

            if (hand.HeldEntity == null)
            {
                // the held item was removed.
                RaiseLocalEvent(held, new HeldVisualsUpdatedEvent(uid, revealedLayers), true);
                return;
            }

            var ev = new GetInhandVisualsEvent(uid, hand.Location);
            RaiseLocalEvent(held, ev, false);

            if (ev.Layers.Count == 0)
            {
                RaiseLocalEvent(held, new HeldVisualsUpdatedEvent(uid, revealedLayers), true);
                return;
            }

            // add the new layers
            foreach (var (key, layerData) in ev.Layers)
            {
                if (!revealedLayers.Add(key))
                {
                    Logger.Warning($"Duplicate key for in-hand visuals: {key}. Are multiple components attempting to modify the same layer? Entity: {ToPrettyString(held)}");
                    continue;
                }

                var index = sprite.LayerMapReserveBlank(key);

                // In case no RSI is given, use the item's base RSI as a default. This cuts down on a lot of unnecessary yaml entries.
                if (layerData.RsiPath == null
                    && layerData.TexturePath == null
                    && sprite[index].Rsi == null
                    && TryComp(held, out SpriteComponent? clothingSprite))
                {
                    sprite.LayerSetRSI(index, clothingSprite.BaseRSI);
                }

                sprite.LayerSetData(index, layerData);
            }

            RaiseLocalEvent(held, new HeldVisualsUpdatedEvent(uid, revealedLayers), true);
        }

        private void OnVisualsChanged(EntityUid uid, HandsComponent component, VisualsChangedEvent args)
        {
            // update hands visuals if this item is in a hand (rather then inventory or other container).
            if (component.Hands.TryGetValue(args.ContainerId, out var hand))
            {
                UpdateHandVisuals(uid, args.Item, hand, component);
            }
        }
        #endregion

        #region Gui
        public void UpdateGui(HandsComponent? hands = null)
        {
            if (hands == null && !TryGetPlayerHands(out hands) || hands.Gui == null)
                return;

            var states = hands.Hands.Values
                .Select(hand => new GuiHand(hand.Name, hand.Location, hand.HeldEntity))
                .ToArray();

            hands.Gui.Update(new HandsGuiState(states, hands.ActiveHand?.Name));
        }

        public override bool TrySetActiveHand(EntityUid uid, string? value, SharedHandsComponent? handComp = null)
        {
            if (!base.TrySetActiveHand(uid, value, handComp))
                return false;

            if (uid == _playerManager.LocalPlayer?.ControlledEntity)
                UpdateGui();

            return true;
        }

        private void HandlePlayerAttached(EntityUid uid, HandsComponent component, PlayerAttachedEvent args)
        {
            component.Gui = new HandsGui(component, this);
            _gameHud.HandsContainer.AddChild(component.Gui);
            component.Gui.SetPositionFirst();
            UpdateGui(component);
        }

        private static void HandlePlayerDetached(EntityUid uid, HandsComponent component, PlayerDetachedEvent args)
        {
            ClearGui(component);
        }

        private static void HandleCompRemove(EntityUid uid, HandsComponent component, ComponentRemove args)
        {
            ClearGui(component);
        }

        private static void ClearGui(HandsComponent comp)
        {
            comp.Gui?.Orphan();
            comp.Gui = null;
        }
        #endregion
    }
}
