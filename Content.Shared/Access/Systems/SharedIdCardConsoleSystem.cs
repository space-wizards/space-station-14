using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;

namespace Content.Shared.Access.Systems
{
    [UsedImplicitly]
    public abstract class SharedIdCardConsoleSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedIdCardConsoleComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SharedIdCardConsoleComponent, ComponentRemove>(OnComponentRemove);
        }

        private void OnComponentInit(EntityUid uid, SharedIdCardConsoleComponent component, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, SharedIdCardConsoleComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
            _itemSlotsSystem.AddItemSlot(uid, SharedIdCardConsoleComponent.TargetIdCardSlotId, component.TargetIdSlot);
        }

        private void OnComponentRemove(EntityUid uid, SharedIdCardConsoleComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.PrivilegedIdSlot);
            _itemSlotsSystem.RemoveItemSlot(uid, component.TargetIdSlot);
        }
    }
}
