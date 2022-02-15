using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.WireHacking;
using Robust.Server.GameObjects;
using Content.Shared.Acts;
using Content.Shared.Item;
using Robust.Shared.Containers;
using static Content.Shared.SmartFridge.SharedSmartFridgeComponent;

namespace Content.Server.SmartFridge
{
    public sealed class SmartFridgeSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        private uint _nextAllocatedId = 0;

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

            if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            {
                TryUpdateVisualState(uid, null, component);
            }
            component.Storage = uid.EnsureContainer<Container>("fridge_entity_container");
        }

        private void OnInventoryRequestMessage(EntityUid uid, SmartFridgeComponent component, InventorySyncRequestMessage args)
        {
            if (!IsPowered(uid, component))
                return;

            component.UserInterface?.SendMessage(new SmartFridgeInventoryMessage(component.Inventory));
        }

        private void OnInventoryEjectMessage(EntityUid uid, SmartFridgeComponent component, SmartFridgeEjectMessage args)
        {
            if (!IsPowered(uid, component))
                return;

            if (args.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;

            TryEjectVendorItem(uid, args.ID, component);
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
            TryUpdateVisualState(uid, SmartFridgeVisualState.Broken, fridgeComponent);
        }

        private void OnInteractUsing(EntityUid uid, SmartFridgeComponent fridgeComponent, InteractUsingEvent args)
        {
            // !TryComp<IStorageComponent>(uid, out var storage)
            // if (TryComp<IStorageComponent>(args.Used, out IStorageComponent? storageComponent))
            // {
            //     TryInsertFromStorage(uid, storageComponent, fridgeComponent);
            // }
            // else
            // {
                TryInsertVendorItem(uid, args.Used, fridgeComponent);
            // }
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

        public void Deny(EntityUid uid, SmartFridgeComponent? fridgeComponent = null)
        {
            if (!Resolve(uid, ref fridgeComponent))
                return;

            SoundSystem.Play(Filter.Pvs(fridgeComponent.Owner), fridgeComponent.SoundDeny.GetSound(), fridgeComponent.Owner, AudioParams.Default.WithVolume(-2f));
            // Play the Deny animation
            TryUpdateVisualState(uid, SmartFridgeVisualState.Deny, fridgeComponent);
            //TODO: This duration should be a distinct value specific to the deny animation
            fridgeComponent.Owner.SpawnTimer(fridgeComponent.AnimationDuration, () =>
            {
                TryUpdateVisualState(uid, SmartFridgeVisualState.Normal, fridgeComponent);
            });
        }

        // public void TryInsertFromStorage(EntityUid uid, EntityStorageComponent storageComponent, SmartFridgeComponent fridgeComponent)
        // {
        //     foreach (var item in storageComponent.Contents.ContainedEntities)
        //     {
        //         var itemInserted = TryInsertVendorItem(uid, item, fridgeComponent);
        //     }
        // }

        public bool TryInsertVendorItem(EntityUid uid, EntityUid itemUid, SmartFridgeComponent fridgeComponent)
        {
            if (fridgeComponent.Storage == null)
                return false;

            if (fridgeComponent.Whitelist != null && !fridgeComponent.Whitelist.IsValid(itemUid))
                return false;

            if (!TryComp<SharedItemComponent>(itemUid, out SharedItemComponent? item))
                return false;

            TryComp<MetaDataComponent>(itemUid, out MetaDataComponent? metaData);
            string name = metaData == null? "Unknown" : metaData.EntityName;

            bool matchedEntry = false;
            foreach (var inventoryItem in fridgeComponent.Inventory)
            {
                if (name == inventoryItem.Name)
                {
                    var listedItem = fridgeComponent.Inventory.Find(x => x.ID == inventoryItem.ID);
                    if (listedItem != null)
                    {
                        matchedEntry = true;
                        listedItem.Amount++;
                        fridgeComponent.entityReference[listedItem.ID].Enqueue(itemUid);
                        break;
                    }
                }
            }

            if (!matchedEntry)
            {
                uint itemID = _nextAllocatedId++;
                SmartFridgeInventoryEntry newEntry = new SmartFridgeInventoryEntry(itemID, name, 1);
                fridgeComponent.entityReference.Add(itemID, new Queue<EntityUid>(new[] {itemUid}));
                fridgeComponent.Inventory.Add(newEntry);
            }

            fridgeComponent.Storage.Insert(itemUid);
            fridgeComponent.UserInterface?.SendMessage(new SmartFridgeInventoryMessage(fridgeComponent.Inventory));
            // SoundSystem.Play(Filter.Pvs(Owner), _soundVend.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
            return true;
        }

        public void TryEjectVendorItem(EntityUid uid, uint itemId, SmartFridgeComponent? fridgeComponent = null)
        {
            if (!Resolve(uid, ref fridgeComponent))
                return;

            if (fridgeComponent.Storage == null || fridgeComponent.Ejecting || fridgeComponent.Inventory == null || fridgeComponent.Inventory.Count == 0 || fridgeComponent.Broken || !IsPowered(uid, fridgeComponent))
                return;

            SmartFridgeInventoryEntry? entry = fridgeComponent.Inventory.Find(x => x.ID == itemId);

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

            fridgeComponent.Ejecting = true;
            entry.Amount--;
            EntityUid targetEntity = fridgeComponent.entityReference[itemId].Dequeue();


            if (entry.Amount == 0 || targetEntity == null)
            {
                fridgeComponent.Inventory.Remove(entry);
                fridgeComponent.entityReference.Remove(itemId);
            }

            fridgeComponent.UserInterface?.SendMessage(new SmartFridgeInventoryMessage(fridgeComponent.Inventory));
            TryUpdateVisualState(uid, SmartFridgeVisualState.Eject, fridgeComponent);
            fridgeComponent.Owner.SpawnTimer(fridgeComponent.VendDelay, () =>
            {
                fridgeComponent.Ejecting = false;
                TryUpdateVisualState(uid, SmartFridgeVisualState.Normal, fridgeComponent);
                fridgeComponent.Storage.Remove(targetEntity);
            });
            SoundSystem.Play(Filter.Pvs(fridgeComponent.Owner), fridgeComponent.SoundVend.GetSound(), fridgeComponent.Owner, AudioParams.Default.WithVolume(-2f));
        }

        public void TryUpdateVisualState(EntityUid uid, SmartFridgeVisualState? state = SmartFridgeVisualState.Normal, SmartFridgeComponent? fridgeComponent = null)
        {
            if (!Resolve(uid, ref fridgeComponent))
                return;

            var finalState = state == null ? SmartFridgeVisualState.Normal : state;
            if (fridgeComponent.Broken)
            {
                finalState = SmartFridgeVisualState.Broken;
            }
            else if (fridgeComponent.Ejecting)
            {
                finalState = SmartFridgeVisualState.Eject;
            }
            else if (!IsPowered(uid, fridgeComponent))
            {
                finalState = SmartFridgeVisualState.Off;
            }

            if (TryComp<AppearanceComponent>(fridgeComponent.Owner, out var appearance))
            {
                appearance.SetData(SmartFridgeVisuals.VisualState, finalState);
            }
        }
    }
}
