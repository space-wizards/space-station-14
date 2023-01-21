using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.UserInterface;
using Content.Server.VendingMachines.Restock;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Emag.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.VendingMachines
{
    public sealed class VendingMachineSystem : SharedVendingMachineSystem
    {
        [Dependency] private readonly IComponentFactory _factory = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _action = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("vending");
            SubscribeLocalEvent<VendingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<VendingMachineComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<VendingMachineComponent, GotEmaggedEvent>(OnEmagged);
            SubscribeLocalEvent<VendingMachineComponent, DamageChangedEvent>(OnDamage);
            SubscribeLocalEvent<VendingMachineComponent, PriceCalculationEvent>(OnVendingPrice);

            SubscribeLocalEvent<VendingMachineComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
            SubscribeLocalEvent<VendingMachineComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
            SubscribeLocalEvent<VendingMachineComponent, VendingMachineEjectMessage>(OnInventoryEjectMessage);

            SubscribeLocalEvent<VendingMachineComponent, VendingMachineSelfDispenseEvent>(OnSelfDispense);

            SubscribeLocalEvent<VendingMachineComponent, VendingMachineRestockEvent>(OnRestock);
        }

        private void OnVendingPrice(EntityUid uid, VendingMachineComponent component, ref PriceCalculationEvent args)
        {
            var price = 0.0;

            foreach (var (id, entry) in component.Inventory)
            {
                if (!_prototypeManager.TryIndex<EntityPrototype>(entry.ID, out var proto))
                {
                    _sawmill.Error($"Unable to find entity prototype {entry.ID} on {ToPrettyString(uid)} vending.");
                    continue;
                }

                price += entry.Amount * _pricing.GetEstimatedPrice(proto, _factory);
            }

            args.Price += price;
        }

        protected override void OnComponentInit(EntityUid uid, VendingMachineComponent component, ComponentInit args)
        {
            base.OnComponentInit(uid, component, args);

            if (HasComp<ApcPowerReceiverComponent>(component.Owner))
            {
                TryUpdateVisualState(uid, component);
            }

            if (component.Action != null)
            {
                var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(component.Action));
                _action.AddAction(uid, action, uid);
            }
        }

        private void OnActivatableUIOpenAttempt(EntityUid uid, VendingMachineComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (component.Broken)
                args.Cancel();
        }

        private void OnBoundUIOpened(EntityUid uid, VendingMachineComponent component, BoundUIOpenedEvent args)
        {
            UpdateVendingMachineInterfaceState(component);
        }

        private void UpdateVendingMachineInterfaceState(VendingMachineComponent component)
        {
            var state = new VendingMachineInterfaceState(GetAllInventory(component.Owner, component));

            _userInterfaceSystem.TrySetUiState(component.Owner, VendingMachineUiKey.Key, state);
        }

        private void OnInventoryEjectMessage(EntityUid uid, VendingMachineComponent component, VendingMachineEjectMessage args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (args.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;

            AuthorizedVend(uid, entity, args.Type, args.ID, component);
        }

        private void OnPowerChanged(EntityUid uid, VendingMachineComponent component, ref PowerChangedEvent args)
        {
            TryUpdateVisualState(uid, component);
        }

        private void OnBreak(EntityUid uid, VendingMachineComponent vendComponent, BreakageEventArgs eventArgs)
        {
            vendComponent.Broken = true;
            TryUpdateVisualState(uid, vendComponent);
        }

        private void OnEmagged(EntityUid uid, VendingMachineComponent component, GotEmaggedEvent args)
        {
            if (component.Emagged || component.EmaggedInventory.Count == 0 )
                return;

            component.Emagged = true;
            args.Handled = true;
        }

        private void OnDamage(EntityUid uid, VendingMachineComponent component, DamageChangedEvent args)
        {
            if (component.Broken || component.DispenseOnHitCoolingDown ||
                component.DispenseOnHitChance == null || args.DamageDelta == null)
                return;

            if (args.DamageIncreased && args.DamageDelta.Total >= component.DispenseOnHitThreshold &&
                _random.Prob(component.DispenseOnHitChance.Value))
            {
                if (component.DispenseOnHitCooldown > 0f)
                    component.DispenseOnHitCoolingDown = true;
                EjectRandom(uid, throwItem: true, forceEject: true, component);
            }
        }

        private void OnSelfDispense(EntityUid uid, VendingMachineComponent component, VendingMachineSelfDispenseEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            EjectRandom(uid, throwItem: true, forceEject: false, component);
        }

        private void OnRestock(EntityUid uid, VendingMachineComponent component, VendingMachineRestockEvent args)
        {
            if (!TryComp<VendingMachineRestockComponent>(args.RestockBox, out var restockComponent))
            {
                _sawmill.Error($"{ToPrettyString(args.User)} tried to restock {ToPrettyString(uid)} with {ToPrettyString(args.RestockBox)} which did not have a VendingMachineRestockComponent.");
                return;
            }

            TryRestockInventory(uid, component);

            _popupSystem.PopupEntity(Loc.GetString("vending-machine-restock-done",
                    ("this", args.RestockBox),
                    ("user", args.User),
                    ("target", uid)),
                args.User,
                PopupType.Medium);

            _audioSystem.PlayPvs(restockComponent.SoundRestockDone, component.Owner,
                AudioParams.Default
                .WithVolume(-2f)
                .WithVariation(0.2f));

            Del(args.RestockBox);
        }

        /// <summary>
        /// Sets the <see cref="VendingMachineComponent.CanShoot"/> property of the vending machine.
        /// </summary>
        public void SetShooting(EntityUid uid, bool canShoot, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.CanShoot = canShoot;
        }

        public void Deny(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (vendComponent.Denying)
                return;

            vendComponent.Denying = true;
            _audioSystem.PlayPvs(vendComponent.SoundDeny, vendComponent.Owner, AudioParams.Default.WithVolume(-2f));
            TryUpdateVisualState(uid, vendComponent);
        }

        /// <summary>
        /// Checks if the user is authorized to use this vending machine
        /// </summary>
        /// <param name="sender">Entity trying to use the vending machine</param>
        public bool IsAuthorized(EntityUid uid, EntityUid? sender, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent) || sender == null)
                return false;

            if (TryComp<AccessReaderComponent?>(vendComponent.Owner, out var accessReader))
            {
                if (!_accessReader.IsAllowed(sender.Value, accessReader) && !vendComponent.Emagged)
                {
                    _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid);
                    Deny(uid, vendComponent);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Tries to eject the provided item. Will do nothing if the vending machine is incapable of ejecting, already ejecting
        /// or the item doesn't exist in its inventory.
        /// </summary>
        /// <param name="type">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        /// <param name="throwItem">Whether the item should be thrown in a random direction after ejection</param>
        public void TryEjectVendorItem(EntityUid uid, InventoryType type, string itemId, bool throwItem, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (vendComponent.Ejecting || vendComponent.Broken || !this.IsPowered(uid, EntityManager))
            {
                return;
            }

            var entry = GetEntry(itemId, type, vendComponent);

            if (entry == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid);
                Deny(uid, vendComponent);
                return;
            }

            if (entry.Amount <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid);
                Deny(uid, vendComponent);
                return;
            }

            if (string.IsNullOrEmpty(entry.ID))
                return;

            if (!TryComp<TransformComponent>(vendComponent.Owner, out var transformComp))
                return;

            // Start Ejecting, and prevent users from ordering while anim playing
            vendComponent.Ejecting = true;
            vendComponent.NextItemToEject = entry.ID;
            vendComponent.ThrowNextItem = throwItem;
            entry.Amount--;
            UpdateVendingMachineInterfaceState(vendComponent);
            TryUpdateVisualState(uid, vendComponent);
            _audioSystem.PlayPvs(vendComponent.SoundVend, vendComponent.Owner, AudioParams.Default.WithVolume(-2f));
        }

        /// <summary>
        /// Checks whether the user is authorized to use the vending machine, then ejects the provided item if true
        /// </summary>
        /// <param name="sender">Entity that is trying to use the vending machine</param>
        /// <param name="type">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        public void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, VendingMachineComponent component)
        {
            if (IsAuthorized(uid, sender, component))
            {
                TryEjectVendorItem(uid, type, itemId, component.CanShoot, component);
            }
        }

        /// <summary>
        /// Tries to update the visuals of the component based on its current state.
        /// </summary>
        public void TryUpdateVisualState(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var finalState = VendingMachineVisualState.Normal;
            if (vendComponent.Broken)
            {
                finalState = VendingMachineVisualState.Broken;
            }
            else if (vendComponent.Ejecting)
            {
                finalState = VendingMachineVisualState.Eject;
            }
            else if (vendComponent.Denying)
            {
                finalState = VendingMachineVisualState.Deny;
            }
            else if (!this.IsPowered(uid, EntityManager))
            {
                finalState = VendingMachineVisualState.Off;
            }

            if (TryComp<AppearanceComponent>(vendComponent.Owner, out var appearance))
            {
                _appearanceSystem.SetData(uid, VendingMachineVisuals.VisualState, finalState, appearance);
            }
        }

        /// <summary>
        /// Ejects a random item from the available stock. Will do nothing if the vending machine is empty.
        /// </summary>
        /// <param name="throwItem">Whether to throw the item in a random direction after dispensing it.</param>
        /// <param name="forceEject">Whether to skip the regular ejection checks and immediately dispense the item without animation.</param>
        public void EjectRandom(EntityUid uid, bool throwItem, bool forceEject = false, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var availableItems = GetAvailableInventory(uid, vendComponent);
            if (availableItems.Count <= 0)
            {
                return;
            }

            var item = _random.Pick(availableItems);

            if (forceEject)
            {
                vendComponent.NextItemToEject = item.ID;
                vendComponent.ThrowNextItem = throwItem;
                var entry = GetEntry(item.ID, item.Type, vendComponent);
                if (entry != null)
                    entry.Amount--;
                EjectItem(vendComponent, forceEject);
            }
            else
                TryEjectVendorItem(uid, item.Type, item.ID, throwItem, vendComponent);
        }

        private void EjectItem(VendingMachineComponent vendComponent, bool forceEject = false)
        {
            // No need to update the visual state because we never changed it during a forced eject
            if (!forceEject)
                TryUpdateVisualState(vendComponent.Owner, vendComponent);

            if (string.IsNullOrEmpty(vendComponent.NextItemToEject))
            {
                vendComponent.ThrowNextItem = false;
                return;
            }

            var ent = EntityManager.SpawnEntity(vendComponent.NextItemToEject, Transform(vendComponent.Owner).Coordinates);
            if (vendComponent.ThrowNextItem)
            {
                var range = vendComponent.NonLimitedEjectRange;
                var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                _throwingSystem.TryThrow(ent, direction, vendComponent.NonLimitedEjectForce);
            }

            vendComponent.NextItemToEject = null;
            vendComponent.ThrowNextItem = false;
        }

        private void DenyItem(VendingMachineComponent vendComponent)
        {
            TryUpdateVisualState(vendComponent.Owner, vendComponent);
        }

        private VendingMachineInventoryEntry? GetEntry(string entryId, InventoryType type, VendingMachineComponent component)
        {
            if (type == InventoryType.Emagged && component.Emagged)
                return component.EmaggedInventory.GetValueOrDefault(entryId);

            if (type == InventoryType.Contraband && component.Contraband)
                return component.ContrabandInventory.GetValueOrDefault(entryId);

            return component.Inventory.GetValueOrDefault(entryId);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in EntityQuery<VendingMachineComponent>())
            {
                if (comp.Ejecting)
                {
                    comp.EjectAccumulator += frameTime;
                    if (comp.EjectAccumulator >= comp.EjectDelay)
                    {
                        comp.EjectAccumulator = 0f;
                        comp.Ejecting = false;

                        EjectItem(comp);
                    }
                }

                if (comp.Denying)
                {
                    comp.DenyAccumulator += frameTime;
                    if (comp.DenyAccumulator >= comp.DenyDelay)
                    {
                        comp.DenyAccumulator = 0f;
                        comp.Denying = false;

                        DenyItem(comp);
                    }
                }

                if (comp.DispenseOnHitCoolingDown)
                {
                    comp.DispenseOnHitAccumulator += frameTime;
                    if (comp.DispenseOnHitAccumulator >= comp.DispenseOnHitCooldown)
                    {
                        comp.DispenseOnHitAccumulator = 0f;
                        comp.DispenseOnHitCoolingDown = false;
                    }
                }
            }
        }

        public void TryRestockInventory(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            RestockInventoryFromPrototype(uid, vendComponent);

            UpdateVendingMachineInterfaceState(vendComponent);
            TryUpdateVisualState(uid, vendComponent);
        }

    }

    public sealed class VendingMachineRestockEvent : EntityEventArgs
    {
        public EntityUid User { get; }
        public EntityUid RestockBox { get; }

        public VendingMachineRestockEvent(EntityUid user, EntityUid restockBox)
        {
            User = user;
            RestockBox = restockBox;
        }
    }
}
