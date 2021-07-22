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
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Notification.Managers;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Server.Hands
{
    [UsedImplicitly]
    internal sealed class HandsSystem : SharedHandsSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandsComponent, ExaminedEvent>(HandleExamined);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.ActivateItemInHand, InputCmdHandler.FromDelegate(HandleActivateItem))
                .Bind(ContentKeyFunctions.ThrowItemInHand, new PointerInputCmdHandler(HandleThrowItem))
                .Bind(ContentKeyFunctions.SmartEquipBackpack, InputCmdHandler.FromDelegate(HandleSmartEquipBackpack))
                .Bind(ContentKeyFunctions.SmartEquipBelt, InputCmdHandler.FromDelegate(HandleSmartEquipBelt))
                .Register<HandsSystem>();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            CommandBinds.Unregister<HandsSystem>();
        }

        protected override void DropAllItemsInHands(IEntity entity, bool doMobChecks = true)
        {
            base.DropAllItemsInHands(entity, doMobChecks);

            if (!entity.TryGetComponent(out IHandsComponent? hands)) return;

            foreach (var heldItem in hands.GetAllHeldItems())
            {
                hands.Drop(heldItem.Owner, doMobChecks, intentional:false);
            }
        }

        //TODO: Actually shows all items/clothing/etc.
        private void HandleExamined(EntityUid uid, HandsComponent component, ExaminedEvent args)
        {
            foreach (var inhand in component.GetAllHeldItems())
            {
                args.Message.AddText($"\n{Loc.GetString("comp-hands-examine", ("user", component.Owner), ("item", inhand.Owner))}");
            }
        }

        protected override void HandleContainerModified(EntityUid uid, SharedHandsComponent component, ContainerModifiedMessage args)
        {
            component.Dirty();
        }

        private bool TryGetHandsComp(ICommonSession? session, [NotNullWhen(true)] out SharedHandsComponent? hands)
        {
            hands = default;

            if (session is not IPlayerSession playerSession)
                return false;

            var playerEnt = playerSession?.AttachedEntity;

            if (playerEnt == null || !playerEnt.IsValid())
                return false;

            playerEnt.TryGetComponent(out hands);
            return hands != null;
        }

        private void HandleActivateItem(ICommonSession? session)
        {
            if (!TryGetHandsComp(session, out var hands))
                return;

            hands.UseActiveHeldEntity();
        }

        private bool HandleThrowItem(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (session is not IPlayerSession playerSession)
                return false;

            var playerEnt = playerSession.AttachedEntity;

            if (playerEnt == null || !playerEnt.IsValid() || !playerEnt.TryGetComponent(out SharedHandsComponent? hands))
                return false;

            if (!hands.TryGetActiveHeldEntity(out var throwEnt))
                return false;

            if (!Get<InteractionSystem>().TryThrowInteraction(hands.Owner, throwEnt))
                return false;

            if (throwEnt.TryGetComponent(out StackComponent? stack) && stack.Count > 1 && stack.ThrowIndividually)
            {
                var splitStack = Get<StackSystem>().Split(throwEnt.Uid, stack, 1, playerEnt.Transform.Coordinates);

                if (splitStack == null)
                    return false;

                throwEnt = splitStack;
            }
            else if (!hands.TryDropEntityToFloor(throwEnt))
                return false;

            var direction = coords.ToMapPos(EntityManager) - playerEnt.Transform.WorldPosition;
            if (direction == Vector2.Zero)
                return true;

            direction = direction.Normalized * Math.Min(direction.Length, hands.ThrowRange);

            var throwStrength = hands.ThrowForceMultiplier;
            throwEnt.TryThrow(direction, throwStrength, playerEnt);

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

            var plyEnt = playerSession.AttachedEntity;

            if (plyEnt == null || !plyEnt.IsValid())
                return;

            if (!plyEnt.TryGetComponent(out SharedHandsComponent? hands) ||
                !plyEnt.TryGetComponent(out InventoryComponent? inventory))
                return;

            if (!inventory.TryGetSlotItem(equipmentSlot, out ItemComponent? equipmentItem) ||
                !equipmentItem.Owner.TryGetComponent(out ServerStorageComponent? storageComponent))
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
                            lastStoredEntity.Transform.Coordinates = plyEnt.Transform.Coordinates;
                    }
                }
            }
        }
    }
}
