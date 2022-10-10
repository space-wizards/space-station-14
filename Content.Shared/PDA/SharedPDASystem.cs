using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Shared.PDA
{
    public abstract class SharedPDASystem : EntitySystem
    {
        [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PDAComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PDAComponent, ComponentRemove>(OnComponentRemove);

            SubscribeLocalEvent<PDAComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
            SubscribeLocalEvent<PDAComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
        }

        protected virtual void OnComponentInit(EntityUid uid, PDAComponent pda, ComponentInit args)
        {
            if (pda.IdCard != null)
                pda.IdSlot.StartingItem = pda.IdCard;

            ItemSlotsSystem.AddItemSlot(uid, PDAComponent.PDAIdSlotId, pda.IdSlot);
            ItemSlotsSystem.AddItemSlot(uid, PDAComponent.PDAPenSlotId, pda.PenSlot);

            UpdatePDAAppearance(pda);
        }

        private void OnComponentRemove(EntityUid uid, PDAComponent pda, ComponentRemove args)
        {
            ItemSlotsSystem.RemoveItemSlot(uid, pda.IdSlot);
            ItemSlotsSystem.RemoveItemSlot(uid, pda.PenSlot);
        }

        protected virtual void OnItemInserted(EntityUid uid, PDAComponent pda, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID == PDAComponent.PDAIdSlotId)
                pda.ContainedID = CompOrNull<IdCardComponent>(args.Entity);

            UpdatePDAAppearance(pda);
        }

        protected virtual void OnItemRemoved(EntityUid uid, PDAComponent pda, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID == pda.IdSlot.ID)
                pda.ContainedID = null;

            UpdatePDAAppearance(pda);
        }

        private void UpdatePDAAppearance(PDAComponent pda)
        {
            if (TryComp(pda.Owner, out AppearanceComponent ? appearance))
                appearance.SetData(PDAVisuals.IDCardInserted, pda.ContainedID != null);
        }
    }
}
