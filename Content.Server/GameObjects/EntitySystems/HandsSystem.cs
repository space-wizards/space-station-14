using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interfaces;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class HandsSystem : SharedHandsSystem
    {
        public override void Initialize()
        {
            base.Initialize();

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

            if (!IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>().TryThrowInteraction(hands.Owner, throwEnt))
                return false;

            if (throwEnt.TryGetComponent(out StackComponent? stack) && stack.Count > 1 && stack.ThrowIndividually)
            {
                stack.Use(1);
                var newThrowEnt = EntityManager.SpawnEntity(throwEnt.Prototype?.ID, playerEnt.Transform.Coordinates);

                if (!throwEnt.TryGetComponent<StackComponent>(out var newStack))
                {
                    Logger.Error($"{newThrowEnt} spawned from throwing {throwEnt} did not have a {nameof(StackComponent)}.");
                    return false;
                }
                newStack.Count = 1;
                throwEnt = newThrowEnt;
            }
            else if (!hands.TryDropEntityToFloor(throwEnt))
                return false;

            var direction = coords.ToMapPos(EntityManager) - playerEnt.Transform.WorldPosition;
            if (direction == Vector2.Zero)
                return true;

            var throwVec = direction.Normalized * MathF.Min(direction.Length, hands.ThrowRange) * hands.ThrowForceMultiplier;
            throwEnt.TryThrow(throwVec, playerEnt);

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
                plyEnt.PopupMessage(Loc.GetString("You have no {0} to take something out of!", SlotNames[equipmentSlot].ToLower()));
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
                    plyEnt.PopupMessage(Loc.GetString("There's nothing in your {0} to take out!", SlotNames[equipmentSlot].ToLower()));
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
