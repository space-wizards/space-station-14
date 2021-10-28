using System;
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

            SubscribeLocalEvent<HandsComponent, PlayerAttachedEvent>(HandlePlayerAttached);
            SubscribeLocalEvent<HandsComponent, PlayerDetachedEvent>(HandlePlayerDetached);
            SubscribeLocalEvent<HandsComponent, ComponentRemove>(HandleCompRemove);
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

        protected override void HandleContainerModified(EntityUid uid, SharedHandsComponent component, ContainerModifiedMessage args)
        {
            if (uid == _playerManager.LocalPlayer?.ControlledEntity?.Uid)
                GuiStateUpdated?.Invoke();
        }

        private void HandlePickupAnimation(PickupAnimationMessage msg)
        {
            if (!EntityManager.TryGetEntity(msg.EntityUid, out var entity))
                return;

            if (!_gameTiming.IsFirstTimePredicted)
                return;

            ReusableAnimations.AnimateEntityPickup(entity, msg.InitialPosition, msg.FinalPosition);
        }

        public HandsGuiState GetGuiState()
        {
            if (GetPlayerHandsComponent() is not { } hands)
                return new HandsGuiState(Array.Empty<GuiHand>());

            var states = hands.Hands
                .Select(hand => new GuiHand(hand.Name, hand.Location, hand.HeldEntity))
                .ToArray();

            return new HandsGuiState(states, hands.ActiveHand);
        }

        public IEntity? GetActiveHandEntity()
        {
            if (GetPlayerHandsComponent() is not { ActiveHand: { } active } hands)
                return null;

            return hands.GetHand(active).HeldEntity;
        }

        private HandsComponent? GetPlayerHandsComponent()
        {
            var player = _playerManager.LocalPlayer?.ControlledEntity;

            if (player == null || !player.TryGetComponent(out HandsComponent? hands))
                return null;

            return hands;
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

        private void HandlePlayerAttached(EntityUid uid, HandsComponent component, PlayerAttachedEvent args)
        {
            component.Gui = new HandsGui(component, this);
            _gameHud.HandsContainer.AddChild(component.Gui);
            component.Gui.SetPositionFirst();
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
    }
}
