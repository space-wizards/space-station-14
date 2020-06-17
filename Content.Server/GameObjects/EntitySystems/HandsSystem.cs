using System.Linq;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Throw;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Players;
using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Strap;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class HandsSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IServerNotifyManager _notifyManager;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

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
                .Bind(ContentKeyFunctions.BuckleEntity, new PointerInputCmdHandler(HandleBuckleEntity))
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

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out T comp))
                return false;

            component = comp;
            return true;
        }

        private static void HandleSwapHands(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
                return;

            var interactionSystem = EntitySystem.Get<InteractionSystem>();

            var oldItem = handsComp.GetActiveHand;

            handsComp.SwapHands();

            var newItem = handsComp.GetActiveHand;

            if(oldItem != null)
                interactionSystem.HandDeselectedInteraction(handsComp.Owner, oldItem.Owner);

            if(newItem != null)
                interactionSystem.HandSelectedInteraction(handsComp.Owner, newItem.Owner);
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
            var targetLength = Math.Min(entToDesiredDropCoords.Length, InteractionSystem.InteractionRange - 0.001f); // InteractionRange is reduced due to InRange not dealing with floating point error
            var newCoords = new GridCoordinates((entToDesiredDropCoords.Normalized * targetLength) + entCoords, coords.GridID);
            var rayLength = EntitySystem.Get<SharedInteractionSystem>().UnobstructedRayLength(ent.Transform.MapPosition, newCoords.ToMap(_mapManager), ignoredEnt: ent);

            handsComp.Drop(handsComp.ActiveIndex, new GridCoordinates(entCoords + (entToDesiredDropCoords.Normalized * rayLength), coords.GridID));

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

            if (!handsComp.CanDrop(handsComp.ActiveIndex))
                return false;

            var throwEnt = handsComp.GetHand(handsComp.ActiveIndex).Owner;

            if (!handsComp.ThrowItem())
                return false;

            // throw the item, split off from a stack if it's meant to be thrown individually
            if (!throwEnt.TryGetComponent(out StackComponent stackComp) || stackComp.Count < 2 || !stackComp.ThrowIndividually)
            {
                handsComp.Drop(handsComp.ActiveIndex);
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

            if (!plyEnt.TryGetComponent(out HandsComponent handsComp) || !plyEnt.TryGetComponent(out InventoryComponent inventoryComp))
                return;

            if (!inventoryComp.TryGetSlotItem(equipementSlot, out ItemComponent equipmentItem)
                || !equipmentItem.Owner.TryGetComponent<ServerStorageComponent>(out var storageComponent))
            {
                _notifyManager.PopupMessage(plyEnt, plyEnt, Loc.GetString("You have no {0} to take something out of!", EquipmentSlotDefines.SlotNames[equipementSlot].ToLower()));
                return;
            }

            var heldItem = handsComp.GetHand(handsComp.ActiveIndex)?.Owner;

            if (heldItem != null)
            {
                storageComponent.PlayerInsertEntity(plyEnt);
            }
            else
            {
                if (storageComponent.StoredEntities.Count == 0)
                {
                    _notifyManager.PopupMessage(plyEnt, plyEnt, Loc.GetString("There's nothing in your {0} to take out!", EquipmentSlotDefines.SlotNames[equipementSlot].ToLower()));
                }
                else
                {
                    var lastStoredEntity = Enumerable.Last(storageComponent.StoredEntities);
                    if (storageComponent.Remove(lastStoredEntity))
                        handsComp.PutInHandOrDrop(lastStoredEntity.GetComponent<ItemComponent>());
                }
            }
        }

        private bool HandleBuckleEntity(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!_mapManager.GridExists(coords.GridID))
            {
                Logger.InfoS("system.interaction", $"Invalid Coordinates: client={session}, coords={coords}");
                return true;
            }

            if (uid.IsClientSide())
            {
                Logger.WarningS("system.interaction",
                    $"Client sent attack with client-side entity. Session={session}, Uid={uid}");
                return true;
            }

            var player = ((IPlayerSession) session).AttachedEntity;

            if (player == null || !player.IsValid())
            {
                return false;
            }

            var plyCoords = player.Transform.GridPosition.Position;
            var plyBucklingCoords = coords.Position - plyCoords;
            // InteractionRange is reduced due to InRange not dealing with floating point error
            var targetLength = Math.Min(plyBucklingCoords.Length, InteractionSystem.InteractionRange - 0.001f);
            var newCoords = new GridCoordinates(plyBucklingCoords.Normalized * targetLength + plyCoords, coords.GridID);
            var rayLength = Get<SharedInteractionSystem>().UnobstructedRayLength(player.Transform.MapPosition,
                newCoords.ToMap(_mapManager), ignoredEnt: player);

            if (!_entityManager.TryGetEntity(uid, out var entity))
            {
                return false;
            }

            if (!entity.TryGetComponent(out BuckleableComponent buckleableComp))
            {
                _notifyManager.PopupMessage(player, player,
                    Loc.GetString("You can't buckle {0:them}!", entity));
                return false;
            }

            var intersecting = _entityManager.GetEntitiesIntersecting(entity, true);
            foreach (var intersect in intersecting)
            {
                if (!intersect.HasComponent<StrapComponent>())
                {
                    continue;
                }

                return buckleableComp.TryBuckle(player, intersect);
            }

            return false;
        }
    }
}
