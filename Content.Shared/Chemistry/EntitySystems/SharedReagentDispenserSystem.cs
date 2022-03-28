using JetBrains.Annotations;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Chemistry.Dispenser;

namespace Content.Shared.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public abstract class SharedReagentDispenserSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedReagentDispenserComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SharedReagentDispenserComponent, ComponentRemove>(OnComponentRemove);
        }

        private void OnComponentInit(EntityUid uid, SharedReagentDispenserComponent component, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, SharedReagentDispenserComponent.BeakerSlotId, component.BeakerSlot);
        }

        private void OnComponentRemove(EntityUid uid, SharedReagentDispenserComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.BeakerSlot);
        }
    }
}
