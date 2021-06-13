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
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Notification;
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
    internal sealed class HandsSystem : EntitySystem
    {
        private const float ThrowForce = 1.5f; // Throwing force of mobs in Newtons

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleContainerModified);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerModified);
            SubscribeLocalEvent<HandsComponent, ExaminedEvent>(HandleExamined);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.SwapHands, InputCmdHandler.FromDelegate(HandleSwapHands))
                .Bind(ContentKeyFunctions.Drop, new PointerInputCmdHandler(HandleDrop))
                .Bind(ContentKeyFunctions.ActivateItemInHand, InputCmdHandler.FromDelegate(HandleActivateItem))
                .Bind(ContentKeyFunctions.ThrowItemInHand, new PointerInputCmdHandler(HandleThrowItem))
                .Bind(ContentKeyFunctions.SmartEquipBackpack, InputCmdHandler.FromDelegate(HandleSmartEquipBackpack))
                .Bind(ContentKeyFunctions.SmartEquipBelt, InputCmdHandler.FromDelegate(HandleSmartEquipBelt))
                .Register<HandsSystem>();
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            CommandBinds.Unregister<HandsSystem>();
        }

        private static void HandleContainerModified(ContainerModifiedMessage args)
        {
            if (args.Container.Owner.TryGetComponent(out IHandsComponent? handsComponent))
            {
                handsComponent.HandleSlotModifiedMaybe(args);
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

        private static bool TryGetAttachedComponent<T>(IPlayerSession? session, [NotNullWhen(true)] out T? component)
            where T : Component
        {
            component = default;

            var ent = session?.AttachedEntity;

            if (ent == null || !ent.IsValid() || !ent.TryGetComponent(out T? comp))
            {
                return false;
            }

            component = comp;
            return true;
        }

        private static void HandleSwapHands(ICommonSession? session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent? handsComp))
            {
                return;
            }

            var interactionSystem = Get<InteractionSystem>();

            var oldItem = handsComp.GetActiveHand;

            handsComp.SwapHands();

            var newItem = handsComp.GetActiveHand;

            if (oldItem != null)
            {
                interactionSystem.HandDeselectedInteraction(handsComp.Owner, oldItem.Owner);
            }

            if (newItem != null)
            {
                interactionSystem.HandSelectedInteraction(handsComp.Owner, newItem.Owner);
            }
        }

        private bool HandleDrop(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            var ent = ((IPlayerSession?) session)?.AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out HandsComponent? handsComp))
                return false;

            if (handsComp.ActiveHand == null || handsComp.GetActiveHand == null)
                return false;

            // It's important to note that the calculations are done in map coordinates (they're absolute).
            // They're translated back to EntityCoordinates at the end.
            var entMap = ent.Transform.MapPosition;
            var targetPos = coords.ToMapPos(EntityManager);
            var dropVector = targetPos - entMap.Position;
            var targetVector = Vector2.Zero;

            if (dropVector != Vector2.Zero)
            {
                var targetLength = MathF.Min(dropVector.Length, SharedInteractionSystem.InteractionRange - 0.001f); // InteractionRange is reduced due to InRange not dealing with floating point error
                var newCoords = new MapCoordinates(dropVector.Normalized * targetLength + entMap.Position, entMap.MapId);
                var rayLength = Get<SharedInteractionSystem>().UnobstructedDistance(entMap, newCoords, ignoredEnt: ent);
                targetVector = dropVector.Normalized * rayLength;
            }

            var resultMapCoordinates = new MapCoordinates(entMap.Position + targetVector, entMap.MapId);
            var resultEntCoordinates = EntityCoordinates.FromMap(coords.GetParent(EntityManager), resultMapCoordinates);
            handsComp.Drop(handsComp.ActiveHand, resultEntCoordinates);

            return true;
        }

        private static void HandleActivateItem(ICommonSession? session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent? handsComp))
                return;

            handsComp.ActivateItem();
        }

        private bool HandleThrowItem(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            var playerEnt = ((IPlayerSession?) session)?.AttachedEntity;

            if (playerEnt == null || !playerEnt.IsValid())
                return false;

            if (!playerEnt.TryGetComponent(out HandsComponent? handsComp))
                return false;

            if (handsComp.ActiveHand == null || !handsComp.CanDrop(handsComp.ActiveHand))
                return false;

            var throwEnt = handsComp.GetItem(handsComp.ActiveHand)?.Owner;

            if (throwEnt == null)
                return false;

            if (!handsComp.ThrowItem())
                return false;

            // throw the item, split off from a stack if it's meant to be thrown individually
            if (!throwEnt.TryGetComponent(out StackComponent? stackComp) || stackComp.Count < 2 || !stackComp.ThrowIndividually)
            {
                handsComp.Drop(handsComp.ActiveHand);
            }
            else
            {
                var splitStack = Get<StackSystem>().Split(throwEnt.Uid, stackComp, 1, playerEnt.Transform.Coordinates);

                if (splitStack == null)
                    return false;

                throwEnt = splitStack;
            }

            var direction = coords.ToMapPos(EntityManager) - playerEnt.Transform.WorldPosition;
            if (direction == Vector2.Zero) return true;

            direction = direction.Normalized * MathF.Min(direction.Length, 8.0f);
            var yeet = direction * ThrowForce * 15;

            // Softer yeet in weightlessness
            if (playerEnt.IsWeightless())
            {
                throwEnt.TryThrow(yeet / 4, playerEnt, 10.0f);
            }
            else
            {
                throwEnt.TryThrow(yeet, playerEnt);
            }

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
            var plyEnt = ((IPlayerSession?) session)?.AttachedEntity;

            if (plyEnt == null || !plyEnt.IsValid())
                return;

            if (!plyEnt.TryGetComponent(out HandsComponent? handsComp) ||
                !plyEnt.TryGetComponent(out InventoryComponent? inventoryComp))
                return;

            if (!inventoryComp.TryGetSlotItem(equipmentSlot, out ItemComponent? equipmentItem)
                || !equipmentItem.Owner.TryGetComponent<ServerStorageComponent>(out var storageComponent))
            {
                plyEnt.PopupMessage(Loc.GetString("hands-system-missing-equipment-slot",
                                                  ("slotName", SlotNames[equipmentSlot].ToLower())));
                return;
            }

            var heldItem = handsComp.GetItem(handsComp.ActiveHand)?.Owner;

            if (heldItem != null)
            {
                storageComponent.PlayerInsertHeldEntity(plyEnt);
            }
            else if (storageComponent.StoredEntities != null)
            {
                if (storageComponent.StoredEntities.Count == 0)
                {
                    plyEnt.PopupMessage(Loc.GetString("hands-system-empty-equipment-slot",
                                                      ("slotName", SlotNames[equipmentSlot].ToLower())));
                }
                else
                {
                    var lastStoredEntity = Enumerable.Last(storageComponent.StoredEntities);
                    if (storageComponent.Remove(lastStoredEntity))
                        handsComp.PutInHandOrDrop(lastStoredEntity.GetComponent<ItemComponent>());
                }
            }
        }
    }
}
