using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Containers.ItemSlot
{
    public class ItemSlotSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ItemSlotComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<ItemSlotComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<ItemSlotComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnComponentInit(EntityUid uid, ItemSlotComponent slot, ComponentInit args)
        {
            slot.ContainerSlot = ContainerHelpers.EnsureContainer<ContainerSlot>(slot.Owner, slot.SlotName);
        }

        private void OnMapInit(EntityUid uid, ItemSlotComponent slot, MapInitEvent args)
        {
            if (!string.IsNullOrEmpty(slot.StartingItem))
            {
                var entManager = slot.Owner.EntityManager;

                var item = entManager.SpawnEntity(slot.StartingItem, slot.Owner.Transform.Coordinates);
                slot.ContainerSlot.Insert(item);
            }
        }

        private void OnInteractUsing(EntityUid uid, ItemSlotComponent slot, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (slot.Whitelist == null || slot.Whitelist.IsValid(args.Used))
                args.Handled = TryInsertContent(slot, args);
        }

        private bool TryInsertContent(ItemSlotComponent slot, InteractUsingEvent eventArgs)
        {
            var item = eventArgs.Used;
            if (slot.ContainerSlot.Contains(item))
                return false;

            if (!eventArgs.User.TryGetComponent(out IHandsComponent? hands))
            {
                slot.Owner.PopupMessage(eventArgs.User, Loc.GetString("item-slot-try-insert-no-hands"));
                return true;
            }

            IEntity? swap = null;
            if (slot.ContainerSlot.ContainedEntity != null)
            {
                // Swap
                swap = slot.ContainerSlot.ContainedEntities[0];
            }

            if (!hands.Drop(item))
            {
                return true;
            }

            if (swap != null)
            {
                hands.PutInHand(swap.GetComponent<ItemComponent>());
            }

            // Insert item
            slot.ContainerSlot.Insert(item);
            RaiseLocalEvent(new ItemSlotChanged(slot, item));
            SoundSystem.Play(Filter.Pvs(slot.Owner), slot.InsertSound.GetSound(), slot.Owner);

            return true;
        }

        public void TryEjectContent(ItemSlotComponent slot, IEntity user)
        {
            if (slot.ContainerSlot.ContainedEntity == null)
                return;

            var pen = slot.ContainerSlot.ContainedEntities[0];
            slot.ContainerSlot.Remove(pen);

            var hands = user.GetComponent<HandsComponent>();
            var itemComponent = pen.GetComponent<ItemComponent>();
            hands.PutInHandOrDrop(itemComponent);

            RaiseLocalEvent(new ItemSlotChanged(slot, null));
            SoundSystem.Play(Filter.Pvs(slot.Owner), slot.EjectSound.GetSound(), slot.Owner);
        }
    }
}
