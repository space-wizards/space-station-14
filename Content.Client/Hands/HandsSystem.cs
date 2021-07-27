using System;
using System.Linq;
using Content.Client.Animations;
using Content.Client.HUD;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Client.Hands
{
    [UsedImplicitly]
    public sealed class HandsSystem : SharedHandsSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public event Action? GuiStateUpdated;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandsComponent, PlayerAttachedEvent>(SetupGui);
            SubscribeLocalEvent<HandsComponent, PlayerDetachedEvent>(ClearGui);
            SubscribeLocalEvent<HandsModifiedMessage>(HandleHandsModified);

            SubscribeNetworkEvent<PickupAnimationMessage>(HandlePickupAnimation);
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<HandsSystem>();
            base.Shutdown();
        }

        private void HandleHandsModified(HandsModifiedMessage ev)
        {
            if (ev.Hands.Owner == _playerManager.LocalPlayer?.ControlledEntity)
                GuiStateUpdated?.Invoke();
        }

        private void HandlePickupAnimation(PickupAnimationMessage msg)
        {
            if (!EntityManager.TryGetEntity(msg.EntityUid, out var entity))
                return;

            if (!_gameTiming.IsFirstTimePredicted)
                return;

            ReusableAnimations.AnimateEntityPickup(entity, msg.InitialPosition, msg.PickupDirection);
        }

        public HandsGuiState GetGuiState()
        {
            var player = _playerManager.LocalPlayer?.ControlledEntity;

            if (player == null || !player.TryGetComponent(out HandsComponent? hands))
                return new HandsGuiState(Array.Empty<GuiHand>());

            var states = hands.ReadOnlyHands
                .Select(hand => new GuiHand(hand.Name, hand.Location, hand.HeldEntity, hand.Enabled))
                .ToArray();

            return new HandsGuiState(states, hands.ActiveHand);
        }

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
                RaiseNetworkEvent(new RequestSetHandEvent(handName));
                return;
            }

            if (pressedHand != activeHand && pressedEntity != null && activeEntity != null)
            {
                // use active item on held item
                RaiseNetworkEvent(new ClientInteractUsingInHandMsg(pressedHand.Name));
                return;
            }

            if (pressedHand != activeHand && pressedEntity != null && activeEntity == null)
            {
                // use active item on held item
                RaiseNetworkEvent(new MoveItemFromHandMsg(pressedHand.Name));
            }
        }

        public void UIHandActivate(string handName)
        {
            RaiseNetworkEvent (new ActivateInHandMsg(handName));
        }

        private void SetupGui(EntityUid uid, HandsComponent component, PlayerAttachedEvent args)
        {
            component.Gui = new HandsGui(component, this);
            _gameHud.HandsContainer.AddChild(component.Gui);
        }

        private static void ClearGui(EntityUid uid, HandsComponent component, PlayerDetachedEvent args)
        {
            component.Gui?.Orphan();
            component.Gui = null;
        }
    }
}
