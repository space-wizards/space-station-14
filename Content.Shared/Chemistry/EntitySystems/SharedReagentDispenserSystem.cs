using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
            _itemSlotsSystem.AddItemSlot(uid, $"{component.Name}-beaker", component.BeakerSlot);
        }

        private void OnComponentRemove(EntityUid uid, SharedReagentDispenserComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.BeakerSlot);
        }
    }
}
