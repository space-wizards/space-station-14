using System.Linq;
using System.Numerics;
using Content.Server.Popups;
using Content.Server.Pulling;
using Content.Server.Stack;
using Content.Server.Storage.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Part;
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Hands.Systems
{
    public sealed class HandsSystem : SharedHandsSystem
    {
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedHandVirtualItemSystem _virtualSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly PullingSystem _pullingSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandsComponent, DisarmedEvent>(OnDisarmed, before: new[] { typeof(StunSystem) });

            SubscribeLocalEvent<HandsComponent, PullStartedMessage>(HandlePullStarted);
            SubscribeLocalEvent<HandsComponent, PullStoppedMessage>(HandlePullStopped);

            SubscribeLocalEvent<HandsComponent, BodyPartAddedEvent>(HandleBodyPartAdded);
            SubscribeLocalEvent<HandsComponent, BodyPartRemovedEvent>(HandleBodyPartRemoved);

            SubscribeLocalEvent<HandsComponent, ComponentGetState>(GetComponentState);

            CommandBinds.Builder
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

        private void GetComponentState(EntityUid uid, HandsComponent hands, ref ComponentGetState args)
        {
            args.State = new HandsComponentState(hands);
        }

        private void OnDisarmed(EntityUid uid, HandsComponent component, DisarmedEvent args)
        {
            if (args.Handled)
                return;

            // Break any pulls
            if (TryComp(uid, out SharedPullerComponent? puller) && puller.Pulling is EntityUid pulled && TryComp(pulled, out SharedPullableComponent? pullable))
                _pullingSystem.TryStopPull(pullable);

            if (!_handsSystem.TryDrop(uid, component.ActiveHand!, null, checkActionBlocker: false))
                return;

            args.Handled = true; // no shove/stun.
        }

        protected override void HandleEntityRemoved(EntityUid uid, HandsComponent hands, EntRemovedFromContainerMessage args)
        {
            base.HandleEntityRemoved(uid, hands, args);

            if (!Deleted(args.Entity) && TryComp(args.Entity, out HandVirtualItemComponent? @virtual))
                _virtualSystem.Delete((args.Entity, @virtual), uid);
        }

        private void HandleBodyPartAdded(EntityUid uid, HandsComponent component, ref BodyPartAddedEvent args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            // If this annoys you, which it should.
            // Ping Smugleaf.
            var location = args.Part.Symmetry switch
            {
                BodyPartSymmetry.None => HandLocation.Middle,
                BodyPartSymmetry.Left => HandLocation.Left,
                BodyPartSymmetry.Right => HandLocation.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(args.Part.Symmetry))
            };

            AddHand(uid, args.Slot, location);
        }

        private void HandleBodyPartRemoved(EntityUid uid, HandsComponent component, ref BodyPartRemovedEvent args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            RemoveHand(uid, args.Slot);
        }

        #region pulling
        private void HandlePullStarted(EntityUid uid, HandsComponent component, PullStartedMessage args)
        {
            if (args.Puller.Owner != uid)
                return;

            if (TryComp<SharedPullerComponent>(args.Puller.Owner, out var pullerComp) && !pullerComp.NeedsHands)
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
            foreach (var hand in component.Hands.Values)
            {
                if (hand.HeldEntity == null
                    || !TryComp(hand.HeldEntity, out HandVirtualItemComponent? virtualItem)
                    || virtualItem.BlockingEntity != args.Pulled.Owner)
                    continue;

                QueueDel(hand.HeldEntity.Value);
                break;
            }
        }
        #endregion

        #region interactions
        private bool HandleThrowItem(ICommonSession? playerSession, EntityCoordinates coordinates, EntityUid entity)
        {
            if (playerSession == null)
                return false;

            if (playerSession.AttachedEntity is not {Valid: true} player ||
                !Exists(player) ||
                ContainerSystem.IsEntityInContainer(player) ||
                !TryComp(player, out HandsComponent? hands) ||
                hands.ActiveHandEntity is not { } throwEnt ||
                !_actionBlockerSystem.CanThrow(player, throwEnt))
                return false;

            if (EntityManager.TryGetComponent(throwEnt, out StackComponent? stack) && stack.Count > 1 && stack.ThrowIndividually)
            {
                var splitStack = _stackSystem.Split(throwEnt, 1, EntityManager.GetComponent<TransformComponent>(player).Coordinates, stack);

                if (splitStack is not {Valid: true})
                    return false;

                throwEnt = splitStack.Value;
            }

            var direction = coordinates.ToMapPos(EntityManager) - Transform(player).WorldPosition;
            if (direction == Vector2.Zero)
                return true;

            direction = direction.Normalized() * Math.Min(direction.Length(), hands.ThrowRange);

            var throwStrength = hands.ThrowForceMultiplier;

            // Let other systems change the thrown entity (useful for virtual items)
            // or the throw strength.
            var ev = new BeforeThrowEvent(throwEnt, direction, throwStrength, player);
            RaiseLocalEvent(player, ev, false);

            if (ev.Handled)
                return true;

            // This can grief the above event so we raise it afterwards
            if (!TryDrop(player, throwEnt, handsComp: hands))
                return false;

            _throwingSystem.TryThrow(ev.ItemUid, ev.Direction, ev.ThrowStrength, ev.PlayerUid);

            return true;
        }
        private void HandleSmartEquipBackpack(ICommonSession? session)
        {
            HandleSmartEquip(session, "back");
        }

        private void HandleSmartEquipBelt(ICommonSession? session)
        {
            HandleSmartEquip(session, "belt");
        }

        // why tf is this even in hands system.
        // TODO: move to storage or inventory
        private void HandleSmartEquip(ICommonSession? session, string equipmentSlot)
        {
            if (session is not { } playerSession)
                return;

            if (playerSession.AttachedEntity is not {Valid: true} plyEnt || !Exists(plyEnt))
                return;

            if (!_actionBlockerSystem.CanInteract(plyEnt, null))
                return;

            if (!TryComp<HandsComponent>(plyEnt, out var hands) ||  hands.ActiveHand == null)
                return;

            if (!_inventorySystem.TryGetSlotEntity(plyEnt, equipmentSlot, out var slotEntity) ||
                !TryComp(slotEntity, out StorageComponent? storageComponent))
            {
                if (_inventorySystem.HasSlot(plyEnt, equipmentSlot))
                {
                    if (hands.ActiveHand.HeldEntity == null && slotEntity != null)
                    {
                        _inventorySystem.TryUnequip(plyEnt, equipmentSlot);
                        PickupOrDrop(plyEnt, slotEntity.Value);
                        return;
                    }
                    if (hands.ActiveHand.HeldEntity == null)
                        return;
                    if (!_inventorySystem.CanEquip(plyEnt, hands.ActiveHand.HeldEntity.Value, equipmentSlot, out var reason))
                    {
                        _popupSystem.PopupEntity(Loc.GetString(reason), plyEnt, session);
                        return;
                    }
                    if (slotEntity == null)
                    {
                        _inventorySystem.TryEquip(plyEnt, hands.ActiveHand.HeldEntity.Value, equipmentSlot);
                        return;
                    }
                    _inventorySystem.TryUnequip(plyEnt, equipmentSlot);
                    _inventorySystem.TryEquip(plyEnt, hands.ActiveHand.HeldEntity.Value, equipmentSlot);
                    PickupOrDrop(plyEnt, slotEntity.Value);
                    return;
                }
                _popupSystem.PopupEntity(Loc.GetString("hands-system-missing-equipment-slot", ("slotName", equipmentSlot)), plyEnt, session);
                return;
            }

            if (hands.ActiveHand.HeldEntity != null)
            {
                _storageSystem.PlayerInsertHeldEntity(slotEntity.Value, plyEnt, storageComponent);
            }
            else
            {
                if (!storageComponent.Container.ContainedEntities.Any())
                {
                    _popupSystem.PopupEntity(Loc.GetString("hands-system-empty-equipment-slot", ("slotName", equipmentSlot)), plyEnt,  session);
                }
                else
                {
                    var lastStoredEntity = storageComponent.Container.ContainedEntities[^1];

                    if (storageComponent.Container.Remove(lastStoredEntity))
                    {
                        PickupOrDrop(plyEnt, lastStoredEntity, animateUser: true, handsComp: hands);
                    }
                }
            }
        }
        #endregion
    }
}
