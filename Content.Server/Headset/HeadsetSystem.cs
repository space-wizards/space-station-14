using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Headset
{
    internal class HeadsetSystem : EntitySystem
    {
        [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HeadsetComponent, ComponentInit>(OnComponentInit);

            SubscribeLocalEvent<HeadsetComponent, InteractUsingEvent>(OnInteractUsingEvent);
            SubscribeLocalEvent<HeadsetComponent, EntInsertedIntoContainerMessage>(OnItemInserted);

        }

        protected virtual void OnComponentInit(EntityUid uid, HeadsetComponent headset, ComponentInit args)
        {

            ItemSlotsSystem.AddItemSlot(uid, $"{headset.Name}", headset.keySlot);

        }

        protected virtual void OnInteractUsingEvent(EntityUid uid, HeadsetComponent headset, InteractUsingEvent args)
        {
            headset.ContainedKey = CompOrNull<EncryptionKeyComponent>(args.Used);
        }

        protected virtual void OnItemInserted(EntityUid uid, HeadsetComponent headset, EntInsertedIntoContainerMessage args)
        {

            if (args.Container.ID == headset.keySlot.ID)
                headset.ContainedKey = CompOrNull<EncryptionKeyComponent>(args.Entity);

        }
    }
}
