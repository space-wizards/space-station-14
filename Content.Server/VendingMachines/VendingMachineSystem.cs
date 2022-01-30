using System.Collections.Generic;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using System;
using System.Linq;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.WireHacking;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Advertise;
using Content.Server.Throwing;
using Robust.Shared.Maths;
using static Content.Shared.VendingMachines.SharedVendingMachineComponent;

namespace Content.Server.VendingMachines.systems
{
    public sealed class VendingMachineSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VendingMachineComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<VendingMachineComponent, ActivateInWorldEvent>(HandleActivate);
            SubscribeLocalEvent<VendingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<VendingMachineComponent, InventorySyncRequestMessage>(OnInventoryRequestMessage);
            SubscribeLocalEvent<VendingMachineComponent, VendingMachineEjectMessage>(OnInventoryEjectMessage);
        }

        private void OnComponentInit(EntityUid uid, VendingMachineComponent component, ComponentInit args)
        {
            base.Initialize();

            if (EntityManager.TryGetComponent(component.Owner, out ApcPowerReceiverComponent? receiver))
            {
                TryUpdateVisualState(uid, receiver.Powered ? VendingMachineVisualState.Normal : VendingMachineVisualState.Off, component);
            }

            InitializeFromPrototype(uid, component);
        }

        private void OnInventoryRequestMessage(EntityUid uid, VendingMachineComponent component, InventorySyncRequestMessage args)
        {
            if (!IsPowered(uid, component))
                return;

            component.UserInterface?.SendMessage(new VendingMachineInventoryMessage(component.Inventory));
        }

        private void OnInventoryEjectMessage(EntityUid uid, VendingMachineComponent component, VendingMachineEjectMessage args)
        {
            if (!IsPowered(uid, component))
                return;

            if (args.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;

            AuthorizedVend(uid, entity, args.ID, component);
        }

        private void HandleActivate(EntityUid uid, VendingMachineComponent component, ActivateInWorldEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            {
                return;
            }

            if (!IsPowered(uid, component))
            {
                return;
            }

            if (EntityManager.TryGetComponent<WiresComponent>(uid, out var wires))
            {
                if (wires.IsPanelOpen)
                {
                    wires.OpenInterface(actor.PlayerSession);
                    return;
                }
            }

            component.UserInterface?.Toggle(actor.PlayerSession);
        }

        private void OnPowerChanged(EntityUid uid, VendingMachineComponent component, PowerChangedEvent args)
        {
            TryUpdateVisualState(uid, null, component);
        }

        public bool IsPowered(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return false;

            if (vendComponent.PowerPulsed
            || vendComponent.PowerCut
            || !EntityManager.TryGetComponent(vendComponent.Owner, out ApcPowerReceiverComponent? receiver))
            {
                return false;
            }
            return receiver.Powered;
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
                var spriteComponent = EntityManager.GetComponent<SpriteComponent>(vendComponent.Owner);
                const string vendingMachineRSIPath = "Structures/Machines/VendingMachines/{0}.rsi";
                spriteComponent.BaseRSIPath = string.Format(vendingMachineRSIPath, vendComponent.SpriteName);
            }
            var inventory = new List<VendingMachineInventoryEntry>();
            foreach (var (id, amount) in packPrototype.StartingInventory)
            {
                if (!_prototypeManager.TryIndex(id, out EntityPrototype? prototype))
                {
                    continue;
                }
                inventory.Add(new VendingMachineInventoryEntry(id, amount));
            }
            vendComponent.Inventory = inventory;
        }

        public void Deny(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            SoundSystem.Play(Filter.Pvs(vendComponent.Owner), vendComponent.SoundDeny.GetSound(), vendComponent.Owner, AudioParams.Default.WithVolume(-2f));
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
            if (!Resolve(uid, ref vendComponent))
                return false;

            if (!vendComponent.AllAccess && EntityManager.TryGetComponent<AccessReaderComponent?>(vendComponent.Owner, out var accessReader))
            {
                if (sender == null || !EntitySystem.Get<AccessReaderSystem>().IsAllowed(accessReader, sender.Value))
                {
                    vendComponent.Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-access-denied"));
                    Deny(uid, vendComponent);
                    return false;
                }
            }
            return true;
        }

        public void TryEjectVendorItem(EntityUid uid, string itemId, bool throwItem, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (vendComponent.Ejecting || vendComponent.Broken || !IsPowered(uid, vendComponent))
            {
                return;
            }

            var entry = vendComponent.Inventory.Find(x => x.ID == itemId);
            if (entry == null)
            {
                vendComponent.Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-invalid-item"));
                Deny(uid, vendComponent);
                return;
            }

            if (entry.Amount <= 0)
            {
                vendComponent.Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-out-of-stock"));
                Deny(uid, vendComponent);
                return;
            }

            if (entry.ID == null)
            { // If this item is not a stored entity, eject as a new entity of type
                return;
            }

            // Start Ejecting, and prevent users from ordering while anim playing
            vendComponent.Ejecting = true;
            entry.Amount--;
            vendComponent.UserInterface?.SendMessage(new VendingMachineInventoryMessage(vendComponent.Inventory));
            TryUpdateVisualState(uid, VendingMachineVisualState.Eject, vendComponent);
            vendComponent.Owner.SpawnTimer(vendComponent.AnimationDuration, () =>
            {
                vendComponent.Ejecting = false;
                TryUpdateVisualState(uid, VendingMachineVisualState.Normal, vendComponent);
                var ent = EntityManager.SpawnEntity(entry.ID, EntityManager.GetComponent<TransformComponent>(vendComponent.Owner).Coordinates);
                if (throwItem)
                {
                    float range = vendComponent.NonLimitedEjectRange;
                    Vector2 direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                    ent.TryThrow(direction, vendComponent.NonLimitedEjectForce);
                }
            });
            SoundSystem.Play(Filter.Pvs(vendComponent.Owner), vendComponent.SoundVend.GetSound(), vendComponent.Owner, AudioParams.Default.WithVolume(-2f));
        }

        public void AuthorizedVend(EntityUid uid, EntityUid sender, string itemId, VendingMachineComponent component)
        {
            if (IsAuthorized(uid, sender, component))
            {
                TryEjectVendorItem(uid, itemId, !component.SpeedLimiter, component);
            }
            return;
        }

        public void TryUpdateVisualState(EntityUid uid, VendingMachineVisualState? state, VendingMachineComponent? vendComponent = null)
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
            else if (!IsPowered(uid, vendComponent))
            {
                finalState = VendingMachineVisualState.Off;
            }

            if (EntityManager.TryGetComponent(vendComponent.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(VendingMachineVisuals.VisualState, finalState);
            }
        }

        public void EjectRandom(EntityUid uid, bool throwItem, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var availableItems = vendComponent.Inventory.Where(x => x.Amount > 0).ToList();
            if (availableItems.Count <= 0)
            {
                return;
            }

            TryEjectVendorItem(uid, _random.Pick(availableItems).ID, throwItem, vendComponent);
        }

        public bool GetAdvertisementState(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return false;

            if (EntityManager.TryGetComponent(vendComponent.Owner, out AdvertiseComponent? advertise))
            {
                return advertise.Enabled;
            }

            return false;
        }
    }
}
