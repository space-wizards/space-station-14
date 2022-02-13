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
using Content.Server.Throwing;
using Robust.Shared.Maths;
using Content.Shared.Acts;

using static Content.Shared.SmartFridge.SharedSmartFridgeComponent;

namespace Content.Server.SmartFridge.systems
{
    public sealed class SmartFridgeSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SmartFridgeComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SmartFridgeComponent, ActivateInWorldEvent>(HandleActivate);
            SubscribeLocalEvent<SmartFridgeComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<SmartFridgeComponent, InventorySyncRequestMessage>(OnInventoryRequestMessage);
            SubscribeLocalEvent<SmartFridgeComponent, SmartFridgeEjectMessage>(OnInventoryEjectMessage);
            SubscribeLocalEvent<SmartFridgeComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<SmartFridgeComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnComponentInit(EntityUid uid, SmartFridgeComponent component, ComponentInit args)
        {
            base.Initialize();

            if (TryComp<ApcPowerReceiverComponent>(component.Owner, out var receiver))
            {
                TryUpdateVisualState(uid, null, component);
            }
        }

        private void OnInventoryRequestMessage(EntityUid uid, SmartFridgeComponent component, InventorySyncRequestMessage args)
        {
            if (!IsPowered(uid, component))
                return;

            component.UserInterface?.SendMessage(new SmartFridgeInventoryMessage(component.PublicInventory));
        }

        private void OnInventoryEjectMessage(EntityUid uid, SmartFridgeComponent component, SmartFridgeEjectMessage args)
        {
            if (!IsPowered(uid, component))
                return;

            if (args.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;

            // TryEjectVendorItem(uid, entity, args.ID, component);
        }

        private void HandleActivate(EntityUid uid, SmartFridgeComponent component, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out var actor))
            {
                return;
            }

            if (!IsPowered(uid, component))
            {
                return;
            }

            if (TryComp<WiresComponent>(uid, out var wires))
            {
                if (wires.IsPanelOpen)
                {
                    wires.OpenInterface(actor.PlayerSession);
                    return;
                }
            }

            component.UserInterface?.Toggle(actor.PlayerSession);
        }

        private void OnPowerChanged(EntityUid uid, SmartFridgeComponent component, PowerChangedEvent args)
        {
            TryUpdateVisualState(uid, null, component);
        }

        private void OnBreak(EntityUid uid, SmartFridgeComponent fridgeComponent, BreakageEventArgs eventArgs)
        {
            fridgeComponent.Broken = true;
            TryUpdateVisualState(uid, VendingMachineVisualState.Broken, fridgeComponent);
        }

        private void OnInteractUsing(EntityUid uid, SmartFridgeComponent fridgeComponent, InteractUsingEvent args)
        {
            // args.
        }


        public void RefreshInventory(EntityUid uid, SmartFridgeComponent? fridgeComponent = null)
        {
            if (!Resolve(uid, ref fridgeComponent))
                return;

            fridgeComponent.PublicInventory = new List<SmartFridgePublicListEntry>();

            if (fridgeComponent.Inventory == null || fridgeComponent.Inventory.Count <= 0)
            {
            }
            else
            {
                foreach (var (identifiers, entry) in fridgeComponent.Inventory)
                {
                    fridgeComponent.PublicInventory.Add(new SmartFridgePublicListEntry(identifiers.ID, identifiers.Name, entry.count));
                }
            }
        }

        public bool IsPowered(EntityUid uid, SmartFridgeComponent? fridgeComponent = null)
        {
            if (!Resolve(uid, ref fridgeComponent))
                return false;

            if (!TryComp<ApcPowerReceiverComponent>(fridgeComponent.Owner, out var receiver))
            {
                return false;
            }
            return receiver.Powered;
        }

        public void InitializeSelection(EntityUid uid, SmartFridgeComponent? fridgeComponent = null)
        {
            if (!Resolve(uid, ref fridgeComponent))
                return;


        }

        public void Deny(EntityUid uid, SmartFridgeComponent? fridgeComponent = null)
        {
            if (!Resolve(uid, ref fridgeComponent))
                return;

            SoundSystem.Play(Filter.Pvs(fridgeComponent.Owner), fridgeComponent.SoundDeny.GetSound(), fridgeComponent.Owner, AudioParams.Default.WithVolume(-2f));
            // Play the Deny animation
            TryUpdateVisualState(uid, VendingMachineVisualState.Deny, fridgeComponent);
            //TODO: This duration should be a distinct value specific to the deny animation
            fridgeComponent.Owner.SpawnTimer(fridgeComponent.AnimationDuration, () =>
            {
                TryUpdateVisualState(uid, VendingMachineVisualState.Normal, fridgeComponent);
            });
        }

        public void TryEjectVendorItem(EntityUid uid, string itemId, bool throwItem, SmartFridgeComponent? fridgeComponent = null)
        {
            if (!Resolve(uid, ref fridgeComponent))
                return;

            if (fridgeComponent.Ejecting || fridgeComponent.Inventory == null || fridgeComponent.Broken || !IsPowered(uid, fridgeComponent))
            {
                return;
            }

            var entry = fridgeComponent.Inventory.Find(x => x.ID == itemId);
            if (entry == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid, Filter.Pvs(uid));
                Deny(uid, fridgeComponent);
                return;
            }

            if (entry.Amount <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid, Filter.Pvs(uid));
                Deny(uid, fridgeComponent);
                return;
            }

            if (entry.ID == null)
                return;

            if (!TryComp<TransformComponent>(fridgeComponent.Owner, out var transformComp))
                return;

            // Start Ejecting, and prevent users from ordering while anim playing
            fridgeComponent.Ejecting = true;
            entry.Amount--;
            fridgeComponent.UserInterface?.SendMessage(new VendingMachineInventoryMessage(fridgeComponent.Inventory));
            TryUpdateVisualState(uid, VendingMachineVisualState.Eject, fridgeComponent);
            fridgeComponent.Owner.SpawnTimer(fridgeComponent.AnimationDuration, () =>
            {
                fridgeComponent.Ejecting = false;
                TryUpdateVisualState(uid, VendingMachineVisualState.Normal, fridgeComponent);
                var ent = EntityManager.SpawnEntity(entry.ID, transformComp.Coordinates);
                if (throwItem)
                {
                    float range = fridgeComponent.NonLimitedEjectRange;
                    Vector2 direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                    ent.TryThrow(direction, fridgeComponent.NonLimitedEjectForce);
                }
            });
            SoundSystem.Play(Filter.Pvs(fridgeComponent.Owner), fridgeComponent.SoundVend.GetSound(), fridgeComponent.Owner, AudioParams.Default.WithVolume(-2f));
        }

        public void TryUpdateVisualState(EntityUid uid, VendingMachineVisualState? state = VendingMachineVisualState.Normal, SmartFridgeComponent? fridgeComponent = null)
        {
            if (!Resolve(uid, ref fridgeComponent))
                return;

            var finalState = state == null ? VendingMachineVisualState.Normal : state;
            if (fridgeComponent.Broken)
            {
                finalState = VendingMachineVisualState.Broken;
            }
            else if (fridgeComponent.Ejecting)
            {
                finalState = VendingMachineVisualState.Eject;
            }
            else if (!IsPowered(uid, fridgeComponent))
            {
                finalState = VendingMachineVisualState.Off;
            }

            if (TryComp<AppearanceComponent>(fridgeComponent.Owner, out var appearance))
            {
                appearance.SetData(VendingMachineVisuals.VisualState, finalState);
            }
        }
    }
}
