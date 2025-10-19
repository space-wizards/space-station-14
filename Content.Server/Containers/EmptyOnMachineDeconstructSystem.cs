using Content.Shared.Construction;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Server.Containers
{
    /// <summary>
    /// Implements functionality of EmptyOnMachineDeconstructComponent.
    /// </summary>
    [UsedImplicitly]
    public sealed class EmptyOnMachineDeconstructSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmptyOnMachineDeconstructComponent, MachineDeconstructedEvent>(OnDeconstruct);
            SubscribeLocalEvent<ItemSlotsComponent, MachineDeconstructedEvent>(OnSlotsDeconstruct);
        }

        // really this should be handled by ItemSlotsSystem, but for whatever reason MachineDeconstructedEvent is server-side? So eh.
        private void OnSlotsDeconstruct(EntityUid uid, ItemSlotsComponent component, MachineDeconstructedEvent args)
        {
            foreach (var slot in component.Slots.Values)
            {
                if (slot.EjectOnDeconstruct && slot.Item != null && slot.ContainerSlot != null)
                    _container.Remove(slot.Item.Value, slot.ContainerSlot);
            }
        }

        private void OnDeconstruct(EntityUid uid, EmptyOnMachineDeconstructComponent component, MachineDeconstructedEvent ev)
        {
            if (!TryComp<ContainerManagerComponent>(uid, out var mComp))
                return;

            var baseCoords = Transform(uid).Coordinates;

            foreach (var v in component.Containers)
            {
                if (_container.TryGetContainer(uid, v, out var container, mComp))
                {
                    _container.EmptyContainer(container, true, baseCoords);
                }
            }
        }
    }
}
