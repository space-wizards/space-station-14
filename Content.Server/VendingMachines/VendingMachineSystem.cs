using System.Linq;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Emag.Systems;
using Content.Shared.Throwing;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using static Content.Shared.VendingMachines.SharedVendingMachineComponent;

namespace Content.Server.VendingMachines
{
    public sealed class VendingMachineSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly SharedActionsSystem _action = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VendingMachineComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<VendingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<VendingMachineComponent, InventorySyncRequestMessage>(OnInventoryRequestMessage);
            SubscribeLocalEvent<VendingMachineComponent, VendingMachineEjectMessage>(OnInventoryEjectMessage);
            SubscribeLocalEvent<VendingMachineComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<VendingMachineComponent, GotEmaggedEvent>(OnEmagged);
            SubscribeLocalEvent<VendingMachineComponent, DamageChangedEvent>(OnDamage);

            SubscribeLocalEvent<VendingMachineComponent, VendingMachineSelfDispenseEvent>(OnSelfDispense);
        }

        private void OnComponentInit(EntityUid uid, VendingMachineComponent component, ComponentInit args)
        {
            base.Initialize();

            if (TryComp<ApcPowerReceiverComponent>(component.Owner, out var receiver))
            {
                TryUpdateVisualState(uid, null, component);
            }

            if (component.Action != null)
            {
                var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(component.Action));
                _action.AddAction(uid, action, uid);
            }

            InitializeFromPrototype(uid, component);
        }

        private void OnInventoryRequestMessage(EntityUid uid, VendingMachineComponent component, InventorySyncRequestMessage args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            var inventory = new List<VendingMachineInventoryEntry>(component.Inventory);

            if (component.Emagged) inventory.AddRange(component.EmaggedInventory);
            if (component.Contraband) inventory.AddRange(component.ContrabandInventory);

            component.UserInterface?.SendMessage(new VendingMachineInventoryMessage(inventory));
        }

        private void OnInventoryEjectMessage(EntityUid uid, VendingMachineComponent component, VendingMachineEjectMessage args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (args.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;

            AuthorizedVend(uid, entity, args.Type, args.ID, component);
        }

        private void OnPowerChanged(EntityUid uid, VendingMachineComponent component, PowerChangedEvent args)
        {
            TryUpdateVisualState(uid, null, component);
        }

        private void OnBreak(EntityUid uid, VendingMachineComponent vendComponent, BreakageEventArgs eventArgs)
        {
            vendComponent.Broken = true;
            TryUpdateVisualState(uid, VendingMachineVisualState.Broken, vendComponent);
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
            if (component.DispenseOnHitChance == null || args.DamageDelta == null)
                return;

            if (args.DamageDelta.Total >= component.DispenseOnHitThreshold && _random.Prob(component.DispenseOnHitChance.Value))
                EjectRandom(uid, true, component);
        }

        private void OnSelfDispense(EntityUid uid, VendingMachineComponent component, VendingMachineSelfDispenseEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            EjectRandom(uid, true, component);
        }

        public void InitializeFromPrototype(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (string.IsNullOrEmpty(vendComponent.PackPrototypeId)) { return; }

            if (!_prototypeManager.TryIndex(vendComponent.PackPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
            {
                return;
            }

            MetaData(uid).EntityName = packPrototype.Name;
            vendComponent.AnimationDuration = TimeSpan.FromSeconds(packPrototype.AnimationDuration);
            vendComponent.SpriteName = packPrototype.SpriteName;
            if (!string.IsNullOrEmpty(vendComponent.SpriteName))
            {
                if (TryComp<SpriteComponent>(vendComponent.Owner, out var spriteComp)) {
                    const string vendingMachineRSIPath = "Structures/Machines/VendingMachines/{0}.rsi";
                    spriteComp.BaseRSIPath = string.Format(vendingMachineRSIPath, vendComponent.SpriteName);
                }
            }

            AddInventoryFromPrototype(uid, packPrototype.StartingInventory, InventoryType.Regular, vendComponent);
            AddInventoryFromPrototype(uid, packPrototype.EmaggedInventory, InventoryType.Emagged, vendComponent);
            AddInventoryFromPrototype(uid, packPrototype.ContrabandInventory, InventoryType.Contraband, vendComponent);
        }

        private void AddInventoryFromPrototype(EntityUid uid, Dictionary<string, uint>? entries,
            InventoryType type,
            VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component) || entries == null)
            {
                return;
            }

            var inventory = new List<VendingMachineInventoryEntry>();

            foreach (var (id, amount) in entries)
            {
                if (_prototypeManager.HasIndex<EntityPrototype>(id))
                {
                    inventory.Add(new VendingMachineInventoryEntry(type, id, amount));
                }
            }

            switch (type)
            {
                case InventoryType.Regular:
                    component.Inventory.AddRange(inventory);
                    break;
                case InventoryType.Emagged:
                    component.EmaggedInventory.AddRange(inventory);
                    break;
                case InventoryType.Contraband:
                    component.ContrabandInventory.AddRange(inventory);
                    break;
            }
        }

        public void Deny(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            SoundSystem.Play(vendComponent.SoundDeny.GetSound(), Filter.Pvs(vendComponent.Owner), vendComponent.Owner, AudioParams.Default.WithVolume(-2f));
            // Play the Deny animation
            TryUpdateVisualState(uid, VendingMachineVisualState.Deny, vendComponent);
            //TODO: This duration should be a distinct value specific to the deny animation
            vendComponent.Owner.SpawnTimer(vendComponent.AnimationDuration, () =>
            {
                TryUpdateVisualState(uid, VendingMachineVisualState.Normal, vendComponent);
            });
        }

        public bool IsAuthorized(EntityUid uid, EntityUid? sender, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent) || sender == null)
                return false;

            if (TryComp<AccessReaderComponent?>(vendComponent.Owner, out var accessReader))
            {
                if (!_accessReader.IsAllowed(sender.Value, accessReader) && !vendComponent.Emagged)
                {
                    _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid, Filter.Pvs(uid));
                    Deny(uid, vendComponent);
                    return false;
                }
            }
            return true;
        }

        public void TryEjectVendorItem(EntityUid uid, InventoryType type, string itemId, bool throwItem, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (vendComponent.Ejecting || vendComponent.Broken || !this.IsPowered(uid, EntityManager))
            {
                return;
            }

            var entry = type switch
            {
                InventoryType.Regular => vendComponent.Inventory.Find(x => x.ID == itemId),
                InventoryType.Emagged when vendComponent.Emagged => vendComponent.EmaggedInventory.Find(x => x.ID == itemId),
                InventoryType.Contraband when vendComponent.Contraband => vendComponent.ContrabandInventory.Find(x => x.ID == itemId),
                _ => null
            };

            if (entry == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid, Filter.Pvs(uid));
                Deny(uid, vendComponent);
                return;
            }

            if (entry.Amount <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid, Filter.Pvs(uid));
                Deny(uid, vendComponent);
                return;
            }

            if (string.IsNullOrEmpty(entry.ID))
                return;

            if (!TryComp<TransformComponent>(vendComponent.Owner, out var transformComp))
                return;

            // Start Ejecting, and prevent users from ordering while anim playing
            vendComponent.Ejecting = true;
            entry.Amount--;
            vendComponent.UserInterface?.SendMessage(new VendingMachineInventoryMessage(vendComponent.AllInventory));
            TryUpdateVisualState(uid, VendingMachineVisualState.Eject, vendComponent);
            vendComponent.Owner.SpawnTimer(vendComponent.AnimationDuration, () =>
            {
                vendComponent.Ejecting = false;
                TryUpdateVisualState(uid, VendingMachineVisualState.Normal, vendComponent);
                var ent = EntityManager.SpawnEntity(entry.ID, transformComp.Coordinates);
                if (throwItem)
                {
                    float range = vendComponent.NonLimitedEjectRange;
                    Vector2 direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                    _throwingSystem.TryThrow(ent, direction, vendComponent.NonLimitedEjectForce);
                }
            });
            SoundSystem.Play(vendComponent.SoundVend.GetSound(), Filter.Pvs(vendComponent.Owner), vendComponent.Owner, AudioParams.Default.WithVolume(-2f));
        }

        public void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, VendingMachineComponent component)
        {
            if (IsAuthorized(uid, sender, component))
            {
                TryEjectVendorItem(uid, type, itemId, component.CanShoot, component);
            }
        }

        public void TryUpdateVisualState(EntityUid uid, VendingMachineVisualState? state = VendingMachineVisualState.Normal, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var finalState = state == null ? VendingMachineVisualState.Normal : state;
            if (vendComponent.Broken)
            {
                finalState = VendingMachineVisualState.Broken;
            }
            else if (vendComponent.Ejecting)
            {
                finalState = VendingMachineVisualState.Eject;
            }
            else if (!this.IsPowered(uid, EntityManager))
            {
                finalState = VendingMachineVisualState.Off;
            }

            if (TryComp<AppearanceComponent>(vendComponent.Owner, out var appearance))
            {
                appearance.SetData(VendingMachineVisuals.VisualState, finalState);
            }
        }

        public void EjectRandom(EntityUid uid, bool throwItem, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var availableItems = vendComponent.AllInventory.Where(x => x.Amount > 0).ToList();
            if (availableItems.Count <= 0)
            {
                return;
            }

            var item = _random.Pick(availableItems);
            TryEjectVendorItem(uid, item.Type, item.ID, throwItem, vendComponent);
        }
    }
}
