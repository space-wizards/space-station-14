using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Wires;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Borgs;

/// <summary>
/// This handles logic, interactions, and UI related to <see cref="BorgChassisComponent"/> and other related components.
/// </summary>
public abstract partial class SharedStationAISystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly ItemSlotsSystem ItemSlots = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AIChassisComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AIChassisComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<AIChassisComponent, EntRemovedFromContainerMessage>(OnRemoved);

    }


    private void OnStartup(EntityUid uid, AIChassisComponent component, ComponentStartup args)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        component.BrainContainer = Container.EnsureContainer<ContainerSlot>(uid, component.BrainContainerId, containerManager);
    }

    protected virtual void OnInserted(EntityUid uid, AIChassisComponent component, EntInsertedIntoContainerMessage args)
    {

    }

    protected virtual void OnRemoved(EntityUid uid, AIChassisComponent component, EntRemovedFromContainerMessage args)
    {

    }

}
