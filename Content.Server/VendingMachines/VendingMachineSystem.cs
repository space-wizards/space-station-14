using System.Numerics;
using Content.Server.Cargo.Systems;
using Content.Server.Emp;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Broke;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DispenseOnHit;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.VendingMachines
{
    public sealed partial class VendingMachineSystem : SharedVendingMachineSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly SharedActionsSystem _action = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly VendingMachineSystem _machineSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("vending");

            SubscribeLocalEvent<VendingMachineInventoryComponent, MapInitEvent>(OnComponentMapInit);
            SubscribeLocalEvent<VendingMachineVisualStateComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<VendingMachineInventoryComponent, PriceCalculationEvent>(OnVendingPrice);
            SubscribeLocalEvent<VendingMachineEjectComponent, VendingMachineEjectMessage>(OnInventoryEjectMessage);
            SubscribeLocalEvent<VendingMachineEjectComponent, VendingMachineSelfDispenseEvent>(OnSelfDispense);
            SubscribeLocalEvent<VendingMachineInventoryComponent, RestockDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<BrokeComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<BrokeComponent, DamageChangedEvent>(OnDamage);
            SubscribeLocalEvent<BrokeComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
            SubscribeLocalEvent<VendingMachineInventoryComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
            SubscribeLocalEvent<VendingMachineRestockComponent, PriceCalculationEvent>(OnPriceCalculation);
            SubscribeLocalEvent<BrokeComponent, EmpPulseEvent>(OnEmpPulse);
            SubscribeLocalEvent<VendingMachineInventoryComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void OnComponentMapInit(EntityUid uid, VendingMachineInventoryComponent component, MapInitEvent args)
        {
            _action.AddAction(uid, ref component.ActionEntity, component.Action, uid);
            Dirty(uid, component);
        }

        private void OnVendingPrice(EntityUid uid,
            VendingMachineInventoryComponent component,
            ref PriceCalculationEvent args)
        {
            var price = 0.0;

            foreach (var items in component.Items.Values)
            {
                foreach (var item in items)
                {
                    if (!PrototypeManager.TryIndex<EntityPrototype>(item.ItemId, out var proto))
                    {
                        _sawmill.Error(
                            $"Unable to find entity prototype {item.ItemId} on {ToPrettyString(uid)} vending.");
                        continue;
                    }

                    price += item.Amount * _pricing.GetEstimatedPrice(proto);
                }
            }

            args.Price += price;
        }

        protected override void OnComponentInit(EntityUid uid,
            VendingMachineInventoryComponent component,
            ComponentInit args)
        {
            base.OnComponentInit(uid, component, args);

            if (HasComp<ApcPowerReceiverComponent>(uid))
            {
                UpdateVisualState(uid);
            }
        }



        private void OnInventoryEjectMessage(EntityUid uid,
            VendingMachineEjectComponent component,
            VendingMachineEjectMessage args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (args.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;

            AuthorizedVend(uid, entity, args.TypeId, args.Id, component);
        }

        private void OnPowerChanged(EntityUid uid,
            VendingMachineVisualStateComponent component,
            ref PowerChangedEvent args)
        {
            TryUpdateVisualState(uid, component);
        }

        private void OnSelfDispense(EntityUid uid,
            VendingMachineEjectComponent component,
            VendingMachineSelfDispenseEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            EjectRandom(uid, throwItem: true, forceEject: false, component);
        }

        private void OnDoAfter(EntityUid uid,
            VendingMachineInventoryComponent component,
            DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Used == null)
                return;

            if (!TryComp<VendingMachineRestockComponent>(args.Args.Used, out var restockComponent))
            {
                _sawmill.Error(
                    $"{ToPrettyString(args.Args.User)} tried to restock {ToPrettyString(uid)} with {ToPrettyString(args.Args.Used.Value)} which did not have a VendingMachineRestockComponent.");
                return;
            }

            TryRestockInventory(uid, component);

            Popup.PopupEntity(
                Loc.GetString("vending-machine-restock-done", ("this", args.Args.Used), ("user", args.Args.User),
                    ("target", uid)), args.Args.User, PopupType.Medium);

            Audio.PlayPvs(restockComponent.SoundRestockDone, uid,
                AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));

            Del(args.Args.Used.Value);

            args.Handled = true;
        }

        /// <summary>
        /// Sets the <see cref="VendingMachineEjectComponent.CanShoot"/> property of the vending machine.
        /// </summary>
        public void SetShooting(EntityUid uid,
            bool canShoot,
            VendingMachineEjectComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.CanShoot = canShoot;
        }

        /// <summary>
        /// Canceling an attempt to get an item
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="vendComponent"></param>
        public void Deny(EntityUid uid,
            VendingMachineEjectComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (vendComponent.Denying)
                return;

            vendComponent.Denying = true;
            vendComponent.DenyCooldown = _timing.CurTime + vendComponent.DenyDelay;

            Audio.PlayPvs(vendComponent.SoundDeny, uid, AudioParams.Default.WithVolume(-2f));

            UpdateVisualState(uid);
        }

        /// <summary>
        /// Checks if the user is authorized to use this vending machine
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="sender">Entity trying to use the vending machine</param>
        /// <param name="vendComponent"></param>
        public bool IsAuthorized(EntityUid uid,
            EntityUid sender,
            VendingMachineEjectComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return false;

            if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
                return true;

            if (_accessReader.IsAllowed(sender, uid, accessReader) || HasComp<EmaggedComponent>(uid))
                return true;

            Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid);
            Deny(uid, vendComponent);

            return false;
        }

        /// <summary>
        /// Tries to eject the provided item. Will do nothing if the vending machine is incapable of ejecting, already ejecting
        /// or the item doesn't exist in its inventory.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="typeId">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        /// <param name="throwItem">Whether the item should be thrown in a random direction after ejection</param>
        /// <param name="vendComponent"></param>
        public void TryEjectVendorItem(EntityUid uid,
            string typeId,
            string itemId,
            bool throwItem,
            VendingMachineEjectComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (!TryComp<BrokeComponent>(uid, out var brokeComponent))
                return;

            if (vendComponent.Ejecting || brokeComponent.Broken || !this.IsPowered(uid, EntityManager))
            {
                return;
            }

            if (!TryComp<VendingMachineInventoryComponent>(uid, out var inventoryComponent))
                return;

            var entry = GetEntry(uid, itemId, typeId, inventoryComponent);

            if (entry == null)
            {
                Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid);
                Deny(uid, vendComponent);
                return;
            }

            if (entry.Amount <= 0)
            {
                Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid);
                Deny(uid, vendComponent);
                return;
            }

            if (string.IsNullOrEmpty(entry.ItemId))
                return;

            // Start Ejecting, and prevent users from ordering while anim playing
            vendComponent.Ejecting = true;
            vendComponent.Cooldown = _timing.CurTime + vendComponent.Delay;
            vendComponent.NextItemToEject = entry.ItemId;
            vendComponent.ThrowNextItem = throwItem;

            entry.Amount--;

            UpdateVendingMachineInterfaceState(uid, inventoryComponent);
            UpdateVisualState(uid);

            Audio.PlayPvs(vendComponent.SoundVend, uid);
        }

        /// <summary>
        /// Checks whether the user is authorized to use the vending machine, then ejects the provided item if true
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="sender">Entity that is trying to use the vending machine</param>
        /// <param name="typeId">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        /// <param name="component"></param>
        public void AuthorizedVend(EntityUid uid,
            EntityUid sender,
            string typeId,
            string itemId,
            VendingMachineEjectComponent component)
        {
            if (IsAuthorized(uid, sender, component))
            {
                TryEjectVendorItem(uid, typeId, itemId, component.CanShoot, component);
            }
        }

        /// <summary>
        /// Ejects a random item from the available stock. Will do nothing if the vending machine is empty.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="throwItem">Whether to throw the item in a random direction after dispensing it.</param>
        /// <param name="forceEject">Whether to skip the regular ejection checks and immediately dispense the item without animation.</param>
        /// <param name="vendComponent"></param>
        public void EjectRandom(EntityUid uid,
            bool throwItem,
            bool forceEject = false,
            VendingMachineEjectComponent? ejectComponent = null)
        {
            if (!Resolve(uid, ref ejectComponent))
                return;

            if (!TryComp<VendingMachineInventoryComponent>(uid, out var inventoryComponent))
                return;

            var availableItems = GetAvailableInventory(uid, inventoryComponent);
            if (availableItems.Count <= 0)
                return;

            var item = _random.Pick(availableItems);

            if (forceEject)
            {
                ejectComponent.NextItemToEject = item.ItemId;
                ejectComponent.ThrowNextItem = throwItem;

                var entry = GetEntry(uid, item.ItemId, item.TypeId, inventoryComponent);

                if (entry != null)
                    entry.Amount--;

                EjectItem(uid, ejectComponent, forceEject);
            }
            else
            {
                TryEjectVendorItem(uid, item.TypeId, item.ItemId, throwItem, ejectComponent);
            }
        }

        private void EjectItem(EntityUid uid,
            VendingMachineEjectComponent? vendComponent = null,
            bool forceEject = false)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            // No need to update the visual state because we never changed it during a forced eject
            if (!forceEject)
                UpdateVisualState(uid);

            if (string.IsNullOrEmpty(vendComponent.NextItemToEject))
            {
                vendComponent.ThrowNextItem = false;
                return;
            }

            var ent = Spawn(vendComponent.NextItemToEject, Transform(uid).Coordinates);
            if (vendComponent.ThrowNextItem)
            {
                var range = vendComponent.NonLimitedEjectRange;
                var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                _throwingSystem.TryThrow(ent, direction, vendComponent.NonLimitedEjectForce);
            }

            vendComponent.NextItemToEject = null;
            vendComponent.ThrowNextItem = false;
        }

        /// <summary>
        /// Get a specific item in all types of inventory
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="entryId"></param>
        /// <param name="typeId"></param>
        /// <param name="inventoryComponent"></param>
        /// <returns></returns>
        private VendingMachineInventoryEntry? GetEntry(EntityUid uid,
            string entryId,
            string typeId,
            VendingMachineInventoryComponent? inventoryComponent = null)
        {
            if (!Resolve(uid, ref inventoryComponent))
                return null;

            switch (typeId)
            {
                case VendingMachinesInventoryTypeNames.Emagged:
                {
                    if (!HasComp<EmaggedComponent>(uid))
                        return null;

                    return FindEntry(inventoryComponent, entryId, typeId);
                }
                case VendingMachinesInventoryTypeNames.Contraband:
                {
                    if (!inventoryComponent.Contraband)
                        return null;

                    return FindEntry(inventoryComponent, entryId, typeId);
                }
                default:
                {
                    return FindEntry(inventoryComponent, entryId, typeId);
                }
            }
        }

        /// <summary>
        /// Find a specific item in all types of inventory
        /// </summary>
        /// <param name="inventoryComponent"></param>
        /// <param name="entryId"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        private VendingMachineInventoryEntry? FindEntry(VendingMachineInventoryComponent inventoryComponent,
            string entryId,
            string typeId)
        {
            var items = inventoryComponent.Items.GetValueOrDefault(typeId);

            if (items == null)
                return null;

            foreach (var item in items)
            {
                if (item.ItemId == entryId)
                    return item;
            }

            return null;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<VendingMachineEjectComponent>();

            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.Ejecting && CheckCooldownIsOver(ref comp.Cooldown, comp.Delay))
                {
                    comp.Ejecting = false;

                    EjectItem(uid, comp);
                }

                if (comp.Denying && CheckCooldownIsOver(ref comp.DenyCooldown, comp.DenyDelay))
                {
                    comp.Denying = false;

                    EjectItem(uid, comp);
                }

                if (!TryComp<DispenseOnHitComponent>(uid, out var onHitComponent))
                    return;

                if (onHitComponent.CoolingDown && CheckCooldownIsOver(ref onHitComponent.Cooldown,
                        onHitComponent.Delay))
                {
                    onHitComponent.CoolingDown = false;
                }
            }

            var disabled =
                EntityQueryEnumerator<EmpDisabledComponent, VendingMachineEjectComponent>();

            while (disabled.MoveNext(out var uid, out _, out var comp))
            {
                if (!TryComp<VendingMachineEmpEjectComponent>(uid, out var empEjectComponent))
                    return;

                if (!TryComp<VendingMachineInventoryComponent>(uid, out var inventoryComponent))
                    return;

                if (empEjectComponent.NextEmpEject < _timing.CurTime)
                {
                    EjectRandom(uid, true, false, comp);

                    var delay = comp.Delay.TotalSeconds;

                    empEjectComponent.NextEmpEject += TimeSpan.FromSeconds(5 * delay);
                }
            }
        }

        private bool CheckCooldownIsOver(ref TimeSpan cooldown, TimeSpan delay)
        {
            if (_timing.CurTime > cooldown)
            {
                cooldown = _timing.CurTime + delay;

                return true;
            }

            return false;
        }

        public void TryRestockInventory(EntityUid uid,
            VendingMachineInventoryComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            RestockInventoryFromPrototype(uid, vendComponent);
            UpdateVendingMachineInterfaceState(uid, vendComponent);
            UpdateVisualState(uid);
        }
    }
}
