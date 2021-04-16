using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interfaces;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects.EntitySystems
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

            UnsubscribeLocalEvent<EntRemovedFromContainerMessage>();
            UnsubscribeLocalEvent<EntInsertedIntoContainerMessage>();

            CommandBinds.Unregister<HandsSystem>();
        }

        private static void HandleContainerModified(ContainerModifiedMessage args)
        {
            if (args.Container.Owner.TryGetComponent(out IHandsComponent? handsComponent))
            {
                handsComponent.HandleSlotModifiedMaybe(args);
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

            var entMap = ent.Transform.MapPosition;
            var targetPos = coords.ToMapPos(EntityManager);
            var dropVector = targetPos - entMap.Position;
            var targetVector = Vector2.Zero;

            if (dropVector != Vector2.Zero)
            {
                var targetLength = MathF.Min(dropVector.Length, SharedInteractionSystem.InteractionRange - 0.001f); // InteractionRange is reduced due to InRange not dealing with floating point error
                var newCoords = coords.WithPosition(dropVector.Normalized * targetLength + entMap.Position).ToMap(EntityManager);
                var rayLength = Get<SharedInteractionSystem>().UnobstructedDistance(entMap, newCoords, ignoredEnt: ent);
                targetVector = dropVector.Normalized * rayLength;
            }

            handsComp.Drop(handsComp.ActiveHand, coords.WithPosition(entMap.Position + targetVector));

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
            var playerEnt = ((IPlayerSession?)session)?.AttachedEntity;

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
                stackComp.Use(1);
                throwEnt = throwEnt.EntityManager.SpawnEntity(throwEnt.Prototype?.ID, playerEnt.Transform.Coordinates);

                // can only throw one item at a time, regardless of what the prototype stack size is.
                if (throwEnt.TryGetComponent<StackComponent>(out var newStackComp))
                    newStackComp.Count = 1;
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
                plyEnt.PopupMessage(Loc.GetString("You have no {0} to take something out of!",
                    SlotNames[equipmentSlot].ToLower()));
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
                    plyEnt.PopupMessage(Loc.GetString("There's nothing in your {0} to take out!",
                        SlotNames[equipmentSlot].ToLower()));
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
