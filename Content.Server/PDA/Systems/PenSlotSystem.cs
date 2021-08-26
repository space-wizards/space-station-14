using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.PDA.Components;
using Content.Server.PDA.Events;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using System;

namespace Content.Server.PDA.Systems
{
    public class PenSlotSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PenSlotComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PenSlotComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<PenSlotComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnComponentInit(EntityUid uid, PenSlotComponent slot, ComponentInit args)
        {
            slot.PenSlot = ContainerHelpers.EnsureContainer<ContainerSlot>(slot.Owner, "pen_slot");
        }

        private void OnMapInit(EntityUid uid, PenSlotComponent slot, MapInitEvent args)
        {
            if (!string.IsNullOrEmpty(slot.StartingPen))
            {
                var entManager = slot.Owner.EntityManager;

                var pen = entManager.SpawnEntity(slot.StartingPen, slot.Owner.Transform.Coordinates);
                slot.PenSlot.Insert(pen);
            }
        }

        private void OnInteractUsing(EntityUid uid, PenSlotComponent slot, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (args.Used.HasTag("Write"))
                args.Handled = TryInsertContent(slot, args);
        }

        private bool TryInsertContent(PenSlotComponent slot, InteractUsingEvent eventArgs)
        {
            var item = eventArgs.Used;
            if (slot.PenSlot.Contains(item))
                return false;

            if (!eventArgs.User.TryGetComponent(out IHandsComponent? hands))
            {
                slot.Owner.PopupMessage(eventArgs.User, Loc.GetString("comp-pda-ui-try-insert-pen-no-hands"));
                return true;
            }

            IEntity? swap = null;
            if (slot.PenSlot.ContainedEntity != null)
            {
                // Swap
                swap = slot.PenSlot.ContainedEntities[0];
            }

            if (!hands.Drop(item))
            {
                return true;
            }

            if (swap != null)
            {
                hands.PutInHand(swap.GetComponent<ItemComponent>());
            }

            // Insert Pen
            slot.PenSlot.Insert(item);
            RaiseLocalEvent(new PenSlotChanged(item));

            return true;
        }

        public void TryEjectContent(PenSlotComponent slot, IEntity user)
        {
            if (slot.PenSlot.ContainedEntity == null)
                return;

            var pen = slot.PenSlot.ContainedEntities[0];
            slot.PenSlot.Remove(pen);

            var hands = user.GetComponent<HandsComponent>();
            var itemComponent = pen.GetComponent<ItemComponent>();
            hands.PutInHandOrDrop(itemComponent);

            RaiseLocalEvent(new PenSlotChanged(null));
        }
    }
}
