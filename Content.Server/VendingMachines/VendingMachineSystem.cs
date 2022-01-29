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
            // SubscribeLocalEvent<VendingMachineComponent,
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
                TryUpdateVisualState(uid, receiver.Powered ? VendingMachineVisualState.Normal : VendingMachineVisualState.Off, component);
            }

            InitializeFromPrototype(uid, component);
        }

        private void HandleActivate(EntityUid uid, VendingMachineComponent component, ActivateInWorldEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            {
                return;
            }
            if (!IsPowered(component.Owner, component)) {
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

        public bool IsPowered(EntityUid uid, VendingMachineComponent vendComp)
        {
            if (vendComp.PowerPulsed
            || vendComp.PowerCut
            || !EntityManager.TryGetComponent(vendComp.Owner, out ApcPowerReceiverComponent? receiver)) {
                return false;
            }
            return receiver.Powered;
        }

        public void InitializeFromPrototype(EntityUid uid, VendingMachineComponent component)
        {
            if (string.IsNullOrEmpty(component.PackPrototypeId)) { return; }
            if (!_prototypeManager.TryIndex(component.PackPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
            {
                return;
            }
            MetaData(uid).EntityName = packPrototype.Name;
            component.AnimationDuration = TimeSpan.FromSeconds(packPrototype.AnimationDuration);
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

        public void Deny(EntityUid uid, VendingMachineComponent component)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundDeny.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2f));
            // Play the Deny animation
            TryUpdateVisualState(uid, VendingMachineVisualState.Deny, component);
            //TODO: This duration should be a distinct value specific to the deny animation
            component.Owner.SpawnTimer(component.AnimationDuration, () =>
            {
                TryUpdateVisualState(uid, VendingMachineVisualState.Normal, component);
            });
        }

        public bool IsAuthorized(EntityUid? sender, VendingMachineComponent component)
        {
            if (!component.AllAccess && EntityManager.TryGetComponent<AccessReaderComponent?>(component.Owner, out var accessReader))
            {
                if (sender == null || !EntitySystem.Get<AccessReaderSystem>().IsAllowed(accessReader, sender.Value))
                {
                    component.Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-access-denied"));
                    Deny(component.Owner, component);
                    return false;
                }
            }
            return true;
        }

        private void TryDispense(EntityUid uid, string id, bool throwItem, VendingMachineComponent component)
        {
            if (component.Ejecting || component.Broken)
            {
                return;
            }
            var entry = component.Inventory.Find(x => x.ID == id);
            if (entry == null)
            {
                component.Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-invalid-item"));
                Deny(uid, component);
                return;
            }
            if (entry.Amount <= 0)
            {
                component.Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-out-of-stock"));
                Deny(uid, component);
                return;
            }
            if (entry.ID != null) { // If this item is not a stored entity, eject as a new entity of type
                TryEjectVendorItem(uid, entry, throwItem || !component.SpeedLimiter, component);
                return;
            }
            return;
        }

        public void TryEjectVendorItem(EntityUid uid, VendingMachineInventoryEntry entry, bool throwItem, VendingMachineComponent component)
        {
            component.Ejecting = true;
            entry.Amount--;
            component.UserInterface?.SendMessage(new VendingMachineInventoryMessage(component.Inventory));
            TryUpdateVisualState(uid, VendingMachineVisualState.Eject, component);
            component.Owner.SpawnTimer(component.AnimationDuration, () =>
            {
                component.Ejecting = false;
                TryUpdateVisualState(uid, VendingMachineVisualState.Normal, component);
                var ent = EntityManager.SpawnEntity(entry.ID, EntityManager.GetComponent<TransformComponent>(component.Owner).Coordinates);
                if (throwItem) {
                    float range = component.NonLimitedEjectRange;
                    Vector2 direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                    ent.TryThrow(direction, component.NonLimitedEjectForce);
                }
            });

            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundVend.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2f));
        }
        public void AuthorizedVend(EntityUid sender, string id, VendingMachineComponent component)
        {
            if (IsAuthorized(sender, component))
            {
                TryDispense(sender, id, !component.SpeedLimiter, component);
            }
            return;
        }
        public void TryUpdateVisualState(EntityUid uid, VendingMachineVisualState? state, VendingMachineComponent component)
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
            else if (IsPowered(component.Owner, component))
            {
                finalState = VendingMachineVisualState.Off;
            }
            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(VendingMachineVisuals.VisualState, finalState);
            }
        }

        public void EjectRandom(EntityUid uid, bool throwItem, VendingMachineComponent component)
        {
            var availableItems = component.Inventory.Where(x => x.Amount > 0).ToList();
            if (availableItems.Count <= 0)
            {
                return;
            }
            TryDispense(uid, _random.Pick(availableItems).ID, throwItem, component);
        }

        public void SetAdvertisementState(EntityUid uid, bool state, VendingMachineComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AdvertiseComponent? advertise))
            {
                EntitySystem.Get<AdvertiseSystem>().SetEnabled(advertise.Owner, state);
            }
        }
        public bool? GetAdvertisementState(EntityUid uid, VendingMachineComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AdvertiseComponent? advertise))
            {
                return advertise.Enabled;
            }
            return null;
        }

        public void SayAdvertisement(EntityUid uid, VendingMachineComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AdvertiseComponent? advertise))
            {
                EntitySystem.Get<AdvertiseSystem>().SayAdvertisement(advertise.Owner);
            }
        }
    }
}
