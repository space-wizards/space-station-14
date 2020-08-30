using System;
using System.Linq;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Throw;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class HandsSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;

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
                .Bind(ContentKeyFunctions.MovePulledObject, new PointerInputCmdHandler(HandleMovePulledObject))
                .Register<HandsSystem>();
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            CommandBinds.Unregister<HandsSystem>();
            base.Shutdown();
        }

        private static void HandleContainerModified(ContainerModifiedMessage args)
        {
            if (args.Container.Owner.TryGetComponent(out IHandsComponent handsComponent))
            {
                handsComponent.HandleSlotModifiedMaybe(args);
            }
        }

        private static bool TryGetAttachedComponent<T>(IPlayerSession session, out T component)
            where T : Component
        {
            component = default;

            var ent = session.AttachedEntity;

            if (ent == null || !ent.IsValid() || !ent.TryGetComponent(out T comp))
            {
                return false;
            }

            component = comp;
            return true;
        }

        private static void HandleSwapHands(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
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

        private bool HandleDrop(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            var ent = ((IPlayerSession) session).AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out HandsComponent handsComp))
                return false;

            if (handsComp.GetActiveHand == null)
                return false;

            var entCoords = ent.Transform.GridPosition.Position;
            var entToDesiredDropCoords = coords.Position - entCoords;
            var targetLength = Math.Min(entToDesiredDropCoords.Length, SharedInteractionSystem.InteractionRange - 0.001f); // InteractionRange is reduced due to InRange not dealing with floating point error
            var newCoords = new GridCoordinates((entToDesiredDropCoords.Normalized * targetLength) + entCoords, coords.GridID);
            var rayLength = Get<SharedInteractionSystem>().UnobstructedDistance(ent.Transform.MapPosition, newCoords.ToMap(_mapManager), ignoredEnt: ent);

            handsComp.Drop(handsComp.ActiveHand, new GridCoordinates(entCoords + (entToDesiredDropCoords.Normalized * rayLength), coords.GridID));

            return true;
        }

        private static void HandleActivateItem(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
                return;

            handsComp.ActivateItem();
        }

        private bool HandleThrowItem(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            var plyEnt = ((IPlayerSession)session).AttachedEntity;

            if (plyEnt == null || !plyEnt.IsValid())
                return false;

            if (!plyEnt.TryGetComponent(out HandsComponent handsComp))
                return false;

            if (!handsComp.CanDrop(handsComp.ActiveHand))
                return false;

            var throwEnt = handsComp.GetItem(handsComp.ActiveHand).Owner;

            if (!handsComp.ThrowItem())
                return false;

            // throw the item, split off from a stack if it's meant to be thrown individually
            if (!throwEnt.TryGetComponent(out StackComponent stackComp) || stackComp.Count < 2 || !stackComp.ThrowIndividually)
            {
                handsComp.Drop(handsComp.ActiveHand);
            }
            else
            {
                stackComp.Use(1);
                throwEnt = throwEnt.EntityManager.SpawnEntity(throwEnt.Prototype.ID, plyEnt.Transform.GridPosition);

                // can only throw one item at a time, regardless of what the prototype stack size is.
                if (throwEnt.TryGetComponent<StackComponent>(out var newStackComp))
                    newStackComp.Count = 1;
            }

            ThrowHelper.ThrowTo(throwEnt, ThrowForce, coords, plyEnt.Transform.GridPosition, false, plyEnt);

            return true;
        }

        private void HandleSmartEquipBackpack(ICommonSession session)
        {
            HandleSmartEquip(session, EquipmentSlotDefines.Slots.BACKPACK);
        }

        private void HandleSmartEquipBelt(ICommonSession session)
        {
            HandleSmartEquip(session, EquipmentSlotDefines.Slots.BELT);
        }

        private void HandleSmartEquip(ICommonSession session, EquipmentSlotDefines.Slots equipementSlot)
        {
            var plyEnt = ((IPlayerSession) session).AttachedEntity;

            if (plyEnt == null || !plyEnt.IsValid())
                return;

            if (!plyEnt.TryGetComponent(out HandsComponent handsComp) ||
                !plyEnt.TryGetComponent(out InventoryComponent inventoryComp))
                return;

            if (!inventoryComp.TryGetSlotItem(equipementSlot, out ItemComponent equipmentItem)
                || !equipmentItem.Owner.TryGetComponent<ServerStorageComponent>(out var storageComponent))
            {
                _notifyManager.PopupMessage(plyEnt, plyEnt,
                    Loc.GetString("You have no {0} to take something out of!",
                        EquipmentSlotDefines.SlotNames[equipementSlot].ToLower()));
                return;
            }

            var heldItem = handsComp.GetItem(handsComp.ActiveHand)?.Owner;

            if (heldItem != null)
            {
                storageComponent.PlayerInsertEntity(plyEnt);
            }
            else
            {
                if (storageComponent.StoredEntities.Count == 0)
                {
                    _notifyManager.PopupMessage(plyEnt, plyEnt,
                        Loc.GetString("There's nothing in your {0} to take out!",
                            EquipmentSlotDefines.SlotNames[equipementSlot].ToLower()));
                }
                else
                {
                    var lastStoredEntity = Enumerable.Last(storageComponent.StoredEntities);
                    if (storageComponent.Remove(lastStoredEntity))
                        handsComp.PutInHandOrDrop(lastStoredEntity.GetComponent<ItemComponent>());
                }
            }
        }

        private bool HandleMovePulledObject(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            var playerEntity = session.AttachedEntity;

            if (playerEntity == null ||
                !playerEntity.TryGetComponent<HandsComponent>(out var hands))
            {
                return false;
            }

            hands.MovePulledObject(playerEntity.Transform.GridPosition, coords);

            return false;
        }
    }
}
