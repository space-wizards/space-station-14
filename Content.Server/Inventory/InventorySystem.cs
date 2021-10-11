using Content.Server.Atmos;
using Content.Server.Inventory.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.Inventory
{
    class InventorySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HumanInventoryControllerComponent, EntRemovedFromContainerMessage>(HandleRemovedFromContainer);
            SubscribeLocalEvent<InventoryComponent, EntRemovedFromContainerMessage>(HandleInvRemovedFromContainer);
            SubscribeLocalEvent<InventoryComponent, HighPressureEvent>(OnHighPressureEvent);
            SubscribeLocalEvent<InventoryComponent, LowPressureEvent>(OnLowPressureEvent);
        }

        private static void HandleInvRemovedFromContainer(EntityUid uid, InventoryComponent component, EntRemovedFromContainerMessage args)
        {
            component.ForceUnequip(args.Container, args.Entity);
        }

        private static void HandleRemovedFromContainer(EntityUid uid, HumanInventoryControllerComponent component, EntRemovedFromContainerMessage args)
        {
            component.CheckUniformExists();
        }

        private void OnHighPressureEvent(EntityUid uid, InventoryComponent component, HighPressureEvent args)
        {
            RelayPressureEvent(component, args);
        }

        private void OnLowPressureEvent(EntityUid uid, InventoryComponent component, LowPressureEvent args)
        {
            RelayPressureEvent(component, args);
        }

        private void RelayPressureEvent<T>(InventoryComponent component, T args) where T : PressureEvent
        {
            foreach (var equipped in component.GetAllHeldItems())
            {
                RaiseLocalEvent(equipped.Uid, args, false);
            }
        }
    }
}
