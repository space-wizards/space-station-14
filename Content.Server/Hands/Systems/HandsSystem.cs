using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.Stack;
using Content.Server.Storage.Components;
using Content.Server.Throwing;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Physics.Pull;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Utility;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Server.Hands.Systems
{
    [UsedImplicitly]
    internal sealed class HandsSystem : SharedHandsSystem
    {
        [Dependency] private readonly InteractionSystem _interactionSystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandsComponent, ExaminedEvent>(HandleExamined);
            SubscribeNetworkEvent<ActivateInHandMsg>(HandleActivateInHand);
            SubscribeNetworkEvent<ClientInteractUsingInHandMsg>(HandleInteractUsingInHand);
            SubscribeNetworkEvent<UseInHandMsg>(HandleUseInHand);
            SubscribeNetworkEvent<MoveItemFromHandMsg>(HandleMoveItemFromHand);

            SubscribeLocalEvent<HandsComponent, PullAttemptMessage>(HandlePullAttempt);
            SubscribeLocalEvent<HandsComponent, PullStartedMessage>(HandlePullStarted);
            SubscribeLocalEvent<HandsComponent, PullStoppedMessage>(HandlePullStopped);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.ActivateItemInHand, InputCmdHandler.FromDelegate(HandleActivateItem))
                .Bind(ContentKeyFunctions.AltActivateItemInHand, InputCmdHandler.FromDelegate(HandleAltActivateItem))
                .Bind(ContentKeyFunctions.ThrowItemInHand, new PointerInputCmdHandler(HandleThrowItem))
                .Bind(ContentKeyFunctions.SmartEquipBackpack, InputCmdHandler.FromDelegate(HandleSmartEquipBackpack))
                .Bind(ContentKeyFunctions.SmartEquipBelt, InputCmdHandler.FromDelegate(HandleSmartEquipBelt))
                .Bind(ContentKeyFunctions.SwapHands, InputCmdHandler.FromDelegate(SwapHandsPressed, handle: false))
                .Bind(ContentKeyFunctions.Drop, new PointerInputCmdHandler(DropPressed))
                .Register<HandsSystem>();
        }

        private static void HandlePullAttempt(EntityUid uid, HandsComponent component, PullAttemptMessage args)
        {
            if (args.Puller.Owner != uid)
                return;

            // Cancel pull if all hands full.
            if (component.Hands.All(hand => !hand.IsEmpty))
                args.Cancelled = true;
        }

        private void HandlePullStarted(EntityUid uid, HandsComponent component, PullStartedMessage args)
        {
            if (args.Puller.Owner != uid)
                return;

            if (!_virtualItemSystem.TrySpawnVirtualItemInHand(args.Pulled.Owner, uid))
            {
                DebugTools.Assert("Unable to find available hand when starting pulling??");
            }
        }

        private void HandlePullStopped(EntityUid uid, HandsComponent component, PullStoppedMessage args)
        {
            if (args.Puller.Owner != uid)
                return;

            // Try find hand that is doing this pull.
            // and clear it.
            foreach (var hand in component.Hands)
            {
                if (hand.HeldEntity == default
                    || !EntityManager.TryGetComponent(hand.HeldEntity, out HandVirtualItemComponent? virtualItem)
                    || virtualItem.BlockingEntity != args.Pulled.Owner)
                    continue;

                EntityManager.DeleteEntity(hand.HeldEntity);
                break;
            }
        }

        private void SwapHandsPressed(ICommonSession? session)
        {
            var player = session?.AttachedEntity;

            if (!player.HasValue || !player.Value.IsValid())
                return;

            if (!EntityManager.TryGetComponent(player.Value, out SharedHandsComponent? hands))
                return;

            if (!hands.TryGetSwapHandsResult(out var nextHand))
                return;

            hands.ActiveHand = nextHand;
        }

        private bool DropPressed(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            var player = session?.AttachedEntity;

            if (!player.HasValue || !player.Value.IsValid())
                return false;

            if (!EntityManager.TryGetComponent(player.Value, out SharedHandsComponent? hands))
                return false;

            var activeHand = hands.ActiveHand;

            if (activeHand == null)
                return false;

            hands.TryDropHand(activeHand, coords);
            return false;
        }

        private void HandleMoveItemFromHand(MoveItemFromHandMsg msg, EntitySessionEventArgs args)
        {
            if (!TryGetHandsComp(args.SenderSession, out var hands))
                return;

            hands.TryMoveHeldEntityToActiveHand(msg.HandName);
        }

        private void HandleUseInHand(UseInHandMsg msg, EntitySessionEventArgs args)
        {
            if (!TryGetHandsComp(args.SenderSession, out var hands))
                return;

            hands.ActivateItem();
        }

        private void HandleInteractUsingInHand(ClientInteractUsingInHandMsg msg, EntitySessionEventArgs args)
        {
            if (!TryGetHandsComp(args.SenderSession, out var hands))
                return;

            hands.InteractHandWithActiveHand(msg.HandName);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            CommandBinds.Unregister<HandsSystem>();
        }

        private void HandleActivateInHand(ActivateInHandMsg msg, EntitySessionEventArgs args)
        {
            if (!TryGetHandsComp(args.SenderSession, out var hands))
                return;

            hands.ActivateHeldEntity(msg.HandName);
        }

        //TODO: Actually shows all items/clothing/etc.
        private void HandleExamined(EntityUid uid, HandsComponent component, ExaminedEvent args)
        {
            foreach (var inhand in component.GetAllHeldItems())
            {
                if (EntityManager.HasComponent<HandVirtualItemComponent>(inhand.Owner))
                    continue;

                args.PushText(Loc.GetString("comp-hands-examine", ("user", component.Owner), ("item", inhand.Owner)));
            }
        }

        private bool TryGetHandsComp(
            ICommonSession? session,
            [NotNullWhen(true)] out SharedHandsComponent? hands)
        {
            hands = default;

            if (session is not IPlayerSession playerSession)
                return false;

            var player = playerSession.AttachedEntity;

            if (player is not {Valid: true})
                return false;

            return EntityManager.TryGetComponent(player, out hands);
        }

        private void HandleActivateItem(ICommonSession? session)
        {
            if (!TryGetHandsComp(session, out var hands))
                return;

            hands.ActivateItem();
        }

        private void HandleAltActivateItem(ICommonSession? session)
        {
            if (!TryGetHandsComp(session, out var hands))
                return;

            hands.ActivateItem(altInteract: true);
        }

        private bool HandleThrowItem(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (session is not IPlayerSession playerSession)
                return false;

            if (playerSession.AttachedEntity is not {Valid: true} player ||
                !EntityManager.EntityExists(player) ||
                player.IsInContainer() ||
                !EntityManager.TryGetComponent(player, out SharedHandsComponent? hands) ||
                !hands.TryGetActiveHeldEntity(out var throwEnt) ||
                !_actionBlockerSystem.CanThrow(player))
                return false;

            if (EntityManager.TryGetComponent(throwEnt, out StackComponent? stack) && stack.Count > 1 && stack.ThrowIndividually)
            {
                var splitStack = _stackSystem.Split(throwEnt, 1, EntityManager.GetComponent<TransformComponent>(player).Coordinates, stack);

                if (splitStack is not {Valid: true})
                    return false;

                throwEnt = splitStack.Value;
            }
            else if (!hands.Drop(throwEnt))
                return false;

            var direction = coords.ToMapPos(EntityManager) - EntityManager.GetComponent<TransformComponent>(player).WorldPosition;
            if (direction == Vector2.Zero)
                return true;

            direction = direction.Normalized * Math.Min(direction.Length, hands.ThrowRange);

            var throwStrength = hands.ThrowForceMultiplier;
            throwEnt.TryThrow(direction, throwStrength, player);

            return true;
        }

        private void HandleSmartEquipBackpack(ICommonSession? session)
        {
            HandleSmartEquip(session, Slots.BACKPACK);
        }

        private void HandleSmartEquipBelt(ICommonSession? session)
        {
            HandleSmartEquip(session, Slots.BELT);
        }

        private void HandleSmartEquip(ICommonSession? session, Slots equipmentSlot)
        {
            if (session is not IPlayerSession playerSession)
                return;

            if (playerSession.AttachedEntity is not {Valid: true} plyEnt || !EntityManager.EntityExists(plyEnt))
                return;

            if (!EntityManager.TryGetComponent(plyEnt, out SharedHandsComponent? hands) ||
                !EntityManager.TryGetComponent(plyEnt, out InventoryComponent? inventory))
                return;

            if (!inventory.TryGetSlotItem(equipmentSlot, out ItemComponent? equipmentItem) ||
                !EntityManager.TryGetComponent(equipmentItem.Owner, out ServerStorageComponent? storageComponent))
            {
                plyEnt.PopupMessage(Loc.GetString("hands-system-missing-equipment-slot", ("slotName", SlotNames[equipmentSlot].ToLower())));
                return;
            }

            if (hands.ActiveHandIsHoldingEntity())
            {
                storageComponent.PlayerInsertHeldEntity(plyEnt);
            }
            else if (storageComponent.StoredEntities != null)
            {
                if (storageComponent.StoredEntities.Count == 0)
                {
                    plyEnt.PopupMessage(Loc.GetString("hands-system-empty-equipment-slot", ("slotName", SlotNames[equipmentSlot].ToLower())));
                }
                else
                {
                    var lastStoredEntity = Enumerable.Last(storageComponent.StoredEntities);
                    if (storageComponent.Remove(lastStoredEntity))
                    {
                        if (!hands.TryPickupEntityToActiveHand(lastStoredEntity))
                            EntityManager.GetComponent<TransformComponent>(lastStoredEntity).Coordinates = EntityManager.GetComponent<TransformComponent>(plyEnt).Coordinates;
                    }
                }
            }
        }
    }
}
