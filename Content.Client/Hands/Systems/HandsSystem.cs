using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Examine;
using Content.Client.Strip;
using Content.Client.Verbs.UI;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Hands.Systems
{
    [UsedImplicitly]
    public sealed class HandsSystem : SharedHandsSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IUserInterfaceManager _ui = default!;

        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly StrippableSystem _stripSys = default!;
        [Dependency] private readonly ExamineSystem _examine = default!;

        public event Action<string, HandLocation>? OnPlayerAddHand;
        public event Action<string>? OnPlayerRemoveHand;
        public event Action<string?>? OnPlayerSetActiveHand;
        public event Action<HandsComponent>? OnPlayerHandsAdded;
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
        private void HandleComponentState(EntityUid uid, HandsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not HandsComponentState state)
                return;

            var handsModified = component.Hands.Count != state.Hands.Count;
            // we need to check that, even if we have the same amount, that the individual hands didn't change.
            if (!handsModified)
            {
                foreach (var hand in component.Hands.Values)
                {
                    if (state.Hands.Contains(hand))
                        continue;
                    handsModified = true;
                    break;
                }
            }

            var manager = EnsureComp<ContainerManagerComponent>(uid);

            if (handsModified)
            {
                List<Hand> addedHands = new();
                foreach (var hand in state.Hands)
                {
                    if (component.Hands.ContainsKey(hand.Name))
                        continue;

                    var container = _containerSystem.EnsureContainer<ContainerSlot>(uid, hand.Name, manager);
                    var newHand = new Hand(hand.Name, hand.Location, container);
                    component.Hands.Add(hand.Name, newHand);
                    addedHands.Add(newHand);
                }

                foreach (var name in component.Hands.Keys)
                {
                    if (!state.HandNames.Contains(name))
                    {
                        RemoveHand(uid, name, component);
                    }
                }

                component.SortedHands.Clear();
                component.SortedHands.AddRange(state.HandNames);
                var sorted = addedHands.OrderBy(hand => component.SortedHands.IndexOf(hand.Name));

                foreach (var hand in sorted)
                {
                    AddHand(uid, hand, component);
                }
            }

            _stripSys.UpdateUi(uid);

            if (component.ActiveHand == null && state.ActiveHand == null)
                return; //edge case

            if (component.ActiveHand != null && state.ActiveHand != component.ActiveHand.Name)
            {
                SetActiveHand(uid, component.Hands[state.ActiveHand!], component);
            }
        }
        #endregion

        public void ReloadHandButtons()
        {
            if (!TryGetPlayerHands(out var hands))
            {
                return;
            }

            OnPlayerHandsAdded?.Invoke(hands);
        }

        public override void DoDrop(EntityUid uid, Hand hand, bool doDropInteraction = true, HandsComponent? hands = null)
        {
            base.DoDrop(uid, hand, doDropInteraction, hands);

            if (TryComp(hand.HeldEntity, out SpriteComponent? sprite))
                sprite.RenderOrder = EntityManager.CurrentTick.Value;
        }

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

        public void UIInventoryExamine(string handName)
        {
            if (!TryGetPlayerHands(out var hands) ||
                !hands.Hands.TryGetValue(handName, out var hand) ||
                hand.HeldEntity is not { Valid: true } entity)
            {
                return;
            }

            _examine.DoExamine(entity);
        }

        /// <summary>
        ///     Called when a user clicks on the little "activation" icon in the hands GUI. This is currently only used
        ///     by storage (backpacks, etc).
        /// </summary>
        public void UIHandOpenContextMenu(string handName)
        {
            if (!TryGetPlayerHands(out var hands) ||
                !hands.Hands.TryGetValue(handName, out var hand) ||
                hand.HeldEntity is not { Valid: true } entity)
            {
                return;
            }

            _ui.GetUIController<VerbMenuUIController>().OpenVerbMenu(entity);
        }

        public void UIHandAltActivateItem(string handName)
        {
            RaisePredictiveEvent(new RequestHandAltInteractEvent(handName));
        }

        #region visuals

        protected override void HandleEntityInserted(EntityUid uid, HandsComponent hands, EntInsertedIntoContainerMessage args)
        {
            base.HandleEntityInserted(uid, hands, args);

            if (!hands.Hands.TryGetValue(args.Container.ID, out var hand))
                return;
            UpdateHandVisuals(uid, args.Entity, hand);
            _stripSys.UpdateUi(uid);

            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            OnPlayerItemAdded?.Invoke(hand.Name, args.Entity);

            if (HasComp<HandVirtualItemComponent>(args.Entity))
                OnPlayerHandBlocked?.Invoke(hand.Name);
        }

        protected override void HandleEntityRemoved(EntityUid uid, HandsComponent hands, EntRemovedFromContainerMessage args)
        {
            base.HandleEntityRemoved(uid, hands, args);

            if (!hands.Hands.TryGetValue(args.Container.ID, out var hand))
                return;
            UpdateHandVisuals(uid, args.Entity, hand);
            _stripSys.UpdateUi(uid);

            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            OnPlayerItemRemoved?.Invoke(hand.Name, args.Entity);

            if (HasComp<HandVirtualItemComponent>(args.Entity))
                OnPlayerHandUnblocked?.Invoke(hand.Name);
        }

        /// <summary>
        ///     Update the players sprite with new in-hand visuals.
        /// </summary>
        private void UpdateHandVisuals(EntityUid uid, EntityUid held, Hand hand, HandsComponent? handComp = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(uid, ref handComp, ref sprite, false))
                return;

            // visual update might involve changes to the entity's effective sprite -> need to update hands GUI.
            if (uid == _playerManager.LocalPlayer?.ControlledEntity)
                OnPlayerItemAdded?.Invoke(hand.Name, held);

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
            RaiseLocalEvent(held, ev);

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
                    Log.Warning($"Duplicate key for in-hand visuals: {key}. Are multiple components attempting to modify the same layer? Entity: {ToPrettyString(held)}");
                    continue;
                }

                var index = sprite.LayerMapReserveBlank(key);

                // In case no RSI is given, use the item's base RSI as a default. This cuts down on a lot of unnecessary yaml entries.
                if (layerData.RsiPath == null
                    && layerData.TexturePath == null
                    && sprite[index].Rsi == null)
                {
                    if (TryComp<ItemComponent>(held, out var itemComponent) && itemComponent.RsiPath != null)
                        sprite.LayerSetRSI(index, itemComponent.RsiPath);
                    else if (TryComp(held, out SpriteComponent? clothingSprite))
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
                UpdateHandVisuals(uid, GetEntity(args.Item), hand, component);
            }
        }
        #endregion

        #region Gui

        private void HandlePlayerAttached(EntityUid uid, HandsComponent component, LocalPlayerAttachedEvent args)
        {
            OnPlayerHandsAdded?.Invoke(component);
        }

        private void HandlePlayerDetached(EntityUid uid, HandsComponent component, LocalPlayerDetachedEvent args)
        {
            OnPlayerHandsRemoved?.Invoke();
        }

        private void OnHandsStartup(EntityUid uid, HandsComponent component, ComponentStartup args)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == uid)
                OnPlayerHandsAdded?.Invoke(component);
        }

        private void OnHandsShutdown(EntityUid uid, HandsComponent component, ComponentShutdown args)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == uid)
                OnPlayerHandsRemoved?.Invoke();
        }
        #endregion

        private void AddHand(EntityUid uid, Hand newHand, HandsComponent? handsComp = null)
        {
            AddHand(uid, newHand.Name, newHand.Location, handsComp);
        }

        public override void AddHand(EntityUid uid, string handName, HandLocation handLocation, HandsComponent? handsComp = null)
        {
            base.AddHand(uid, handName, handLocation, handsComp);

            if (uid == _playerManager.LocalPlayer?.ControlledEntity)
                OnPlayerAddHand?.Invoke(handName, handLocation);

            if (handsComp == null)
                return;

            if (handsComp.ActiveHand == null)
                SetActiveHand(uid, handsComp.Hands[handName], handsComp);
        }
        public override void RemoveHand(EntityUid uid, string handName, HandsComponent? handsComp = null)
        {
            if (uid == _playerManager.LocalPlayer?.ControlledEntity && handsComp != null &&
                handsComp.Hands.ContainsKey(handName) && uid ==
                _playerManager.LocalPlayer?.ControlledEntity)
            {
                OnPlayerRemoveHand?.Invoke(handName);
            }

            base.RemoveHand(uid, handName, handsComp);
        }

        private void OnHandActivated(Entity<HandsComponent>? ent)
        {
            if (ent is not { } hand)
                return;

            if (_playerManager.LocalPlayer?.ControlledEntity != hand.Owner)
                return;

            if (hand.Comp.ActiveHand == null)
            {
                OnPlayerSetActiveHand?.Invoke(null);
                return;
            }

            OnPlayerSetActiveHand?.Invoke(hand.Comp.ActiveHand.Name);
        }
    }
}
