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
        [Dependency] private readonly AdvertiseSystem _advertisementSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VendingMachineComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<VendingMachineComponent, ActivateInWorldEvent>(HandleActivate);
            SubscribeLocalEvent<VendingMachineComponent, PowerChangedEvent>(OnPowerChanged);
        }

        private void OnComponentInit(EntityUid uid, VendingMachineComponent component, ComponentInit args)
        {
            base.Initialize();

            if (component.UserInterface != null)
            {
                component.UserInterface.OnReceiveMessage += component.OnUiReceiveMessage;
            }
            if (EntityManager.TryGetComponent(component.Owner, out ApcPowerReceiverComponent? receiver))
            {
                TryUpdateVisualState(component, receiver.Powered ? VendingMachineVisualState.Normal : VendingMachineVisualState.Off);
            }

            InitializeFromPrototype(component);
        }

        private void HandleActivate(EntityUid uid, VendingMachineComponent component, ActivateInWorldEvent args)
        {
            if (!component.Powered ||
                !EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            {
                return;
            }
            var wires = EntityManager.GetComponent<WiresComponent>(component.Owner);
            if (wires.IsPanelOpen)
            {
                wires.OpenInterface(actor.PlayerSession);
            } else
            {
                component.UserInterface?.Toggle(actor.PlayerSession);
            }
        }

        private void OnPowerChanged(EntityUid uid, VendingMachineComponent component, PowerChangedEvent args)
        {
            TryUpdateVisualState(component);
        }

        public void InitializeFromPrototype(VendingMachineComponent component)
        {
            if (string.IsNullOrEmpty(component.PackPrototypeId)) { return; }
            if (!_prototypeManager.TryIndex(component.PackPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
            {
                return;
            }
            EntityManager.GetComponent<MetaDataComponent>(component.Owner).EntityName = packPrototype.Name;
            component._animationDuration = TimeSpan.FromSeconds(packPrototype.AnimationDuration);
            component.SpriteName = packPrototype.SpriteName;
            if (!string.IsNullOrEmpty(component.SpriteName))
            {
                var spriteComponent = EntityManager.GetComponent<SpriteComponent>(component.Owner);
                const string vendingMachineRSIPath = "Structures/Machines/VendingMachines/{0}.rsi";
                spriteComponent.BaseRSIPath = string.Format(vendingMachineRSIPath, component.SpriteName);
            }
            var inventory = new List<VendingMachineInventoryEntry>();
            foreach(var (id, amount) in packPrototype.StartingInventory)
            {
                if(!_prototypeManager.TryIndex(id, out EntityPrototype? prototype))
                {
                    continue;
                }
                inventory.Add(new VendingMachineInventoryEntry(id, amount));
            }
            component.Inventory = inventory;
        }

        public void Deny(VendingMachineComponent component)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundDeny.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2f));

            // Play the Deny animation
            TryUpdateVisualState(component, VendingMachineVisualState.Deny);
            //TODO: This duration should be a distinct value specific to the deny animation
            component.Owner.SpawnTimer(component._animationDuration, () =>
            {
                TryUpdateVisualState(component, VendingMachineVisualState.Normal);
            });
        }

        public bool IsAuthorized(VendingMachineComponent component, EntityUid? sender)
        {
            if (!component.AllAccess && EntityManager.TryGetComponent<AccessReaderComponent?>(component.Owner, out var accessReader))
            {
                var accessSystem = EntitySystem.Get<AccessReaderSystem>();
                if (sender == null || !accessSystem.IsAllowed(accessReader, sender.Value))
                {
                    component.Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-access-denied"));
                    Deny(component);
                    return false;
                }
            }
            return true;
        }

        private void TryDispense(VendingMachineComponent component, string id, bool throwItem = false)
        {
            if (component.Ejecting || component.Broken)
            {
                return;
            }
            var entry = component.Inventory.Find(x => x.ID == id);
            if (entry == null)
            {
                component.Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-invalid-item"));
                Deny(component);
                return;
            }
            if (entry.Amount <= 0)
            {
                component.Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-out-of-stock"));
                Deny(component);
                return;
            }
            if (entry.ID != null) { // If this item is not a stored entity, eject as a new entity of type
                TryEjectVendorItem(component, entry, throwItem || !component.SpeedLimiter);
                return;
            }
            return;
        }

        public void TryEjectVendorItem(VendingMachineComponent component, VendingMachineInventoryEntry entry, bool throwItem = false)
        {
            component.Ejecting = true;
            entry.Amount--;
            component.UserInterface?.SendMessage(new VendingMachineInventoryMessage(component.Inventory));
            TryUpdateVisualState(component, VendingMachineVisualState.Eject);
            component.Owner.SpawnTimer(component._animationDuration, () =>
            {
                component.Ejecting = false;
                TryUpdateVisualState(component, VendingMachineVisualState.Normal);
                var ent = EntityManager.SpawnEntity(entry.ID, EntityManager.GetComponent<TransformComponent>(component.Owner).Coordinates);
                if (throwItem) {
                    float range = component.NonLimitedEjectRange;
                    Vector2 direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                    ent.TryThrow(direction, component.NonLimitedEjectForce);
                }
            });

            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundVend.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2f));
        }
        public void AuthorizedVend(VendingMachineComponent component, string id, EntityUid? sender)
        {
            if (IsAuthorized(component, sender))
            {
                TryDispense(component, id);
            }
            return;
        }
        public void TryUpdateVisualState(VendingMachineComponent component, VendingMachineVisualState state = VendingMachineVisualState.Normal)
        {
            var finalState = state;
            if (component.Broken)
            {
                finalState = VendingMachineVisualState.Broken;
            }
            else if (component.Ejecting)
            {
                finalState = VendingMachineVisualState.Eject;
            }
            else if (!component.Powered)
            {
                finalState = VendingMachineVisualState.Off;
            }
            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(VendingMachineVisuals.VisualState, finalState);
            }
        }

        public void EjectRandom(VendingMachineComponent component)
        {
            var availableItems = component.Inventory.Where(x => x.Amount > 0).ToList();
            if (availableItems.Count <= 0)
            {
                return;
            }
            TryDispense(component, _random.Pick(availableItems).ID, true);
        }

        public void SetAdvertisementState(VendingMachineComponent component, bool state)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AdvertiseComponent? advertise))
            {
                _advertisementSystem.SetEnabled(advertise.Owner, state);
            }
        }
        public bool? GetAdvertisementState(VendingMachineComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AdvertiseComponent? advertise))
            {
                return advertise.Enabled;
            }
            return null;
        }

        public void SayAdvertisement(VendingMachineComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AdvertiseComponent? advertise))
            {
                _advertisementSystem.SayAdvertisement(advertise.Owner);
            }
        }
    }
}
