using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Animations;
using Content.Client.HUD;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Hands
{
    [UsedImplicitly]
    public sealed class HandsSystem : SharedHandsSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandsComponent, PlayerAttachedEvent>(HandlePlayerAttached);
            SubscribeLocalEvent<HandsComponent, PlayerDetachedEvent>(HandlePlayerDetached);
            SubscribeLocalEvent<HandsComponent, ComponentRemove>(HandleCompRemove);
            SubscribeLocalEvent<HandsComponent, ComponentHandleState>(HandleComponentState);

            SubscribeNetworkEvent<PickupAnimationEvent>(HandlePickupAnimation);
        }

        #region StateHandling
        private void HandleComponentState(EntityUid uid, HandsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not HandsComponentState state)
                return;

            // Do we have a NEW hand?
            var handsModified = component.Hands.Count != state.Hands.Count;
            if (!handsModified)
            {
                for (var i = 0; i < state.Hands.Count; i++)
                {
                    if (component.Hands[i].Name != state.Hands[i].Name ||
                        component.Hands[i].Location != state.Hands[i].Location)
                    {
                        handsModified = true;
                        break;
                    }
                }
            }

            if (handsModified)
            {
                // we have new hands, get the new containers.
                component.Hands = state.Hands;
                UpdateHandContainers(uid, component);
            }

            TrySetActiveHand(uid, state.ActiveHand, component);
        }

        /// <summary>
        ///     Used to update the hand-containers when hands have been added or removed. Also updates the GUI
        /// </summary>
        public void UpdateHandContainers(EntityUid uid, HandsComponent? hands = null, ContainerManagerComponent? containerMan = null)
        {
            if (!Resolve(uid, ref hands, ref containerMan))
                return;

            foreach (var hand in hands.Hands)
            {
                if (hand.Container == null)
                {
                    hand.Container = hands.Owner.EnsureContainer<ContainerSlot>(hand.Name);
                }
            }

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
            return TryGetPlayerHands(out var hands) && hands.TryGetActiveHeldEntity(out var entity)
                ? entity
                : null;
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
            if (!hands.TryGetHand(handName, out var pressedHand))
                return;

            if (!hands.TryGetActiveHand(out var activeHand))
                return;

            var pressedEntity = pressedHand.HeldEntity;
            var activeEntity = activeHand.HeldEntity;

            if (pressedHand == activeHand && activeEntity != null)
            {
                // use item in hand
                // it will always be attack_self() in my heart.
                RaiseNetworkEvent(new UseInHandMsg());
                return;
            }

            if (pressedHand != activeHand && pressedEntity == null)
            {
                // change active hand
                EntityManager.RaisePredictiveEvent(new RequestSetHandEvent(handName));
                return;
            }

            if (pressedHand != activeHand && pressedEntity != null && activeEntity != null)
            {
                // use active item on held item
                RaiseNetworkEvent(new ClientInteractUsingInHandMsg(pressedHand.Name));
                return;
            }

            if (pressedHand != activeHand && pressedEntity != default && activeEntity == default)
            {
                // use active item on held item
                RaiseNetworkEvent(new MoveItemFromHandMsg(pressedHand.Name));
            }
        }

        /// <summary>
        ///     Called when a user clicks on an item in their hands GUI.
        /// </summary>
        public void UIHandActivate(string handName)
        {
            RaiseNetworkEvent(new ActivateInHandMsg(handName));
        }

        #region Gui
        public void UpdateGui(HandsComponent? hands = null)
        {
            if (hands == null && !TryGetPlayerHands(out hands) || hands.Gui == null)
                return;

            var states = hands.Hands
                .Select(hand => new GuiHand(hand.Name, hand.Location, hand.HeldEntity))
                .ToArray();

            hands.Gui.Update(new HandsGuiState(states, hands.ActiveHand));
        }

        public override void UpdateHandVisuals(EntityUid uid, SharedHandsComponent? handComp = null, AppearanceComponent? appearance = null)
        {
            base.UpdateHandVisuals(uid, handComp, appearance);

            if (uid == _playerManager.LocalPlayer?.ControlledEntity)
                UpdateGui();
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
