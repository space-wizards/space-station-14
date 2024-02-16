using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.AI;

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

        SubscribeLocalEvent<StationAIComponent, ComponentStartup>(OnStartup);

    }


    private void OnStartup(EntityUid uid, StationAIComponent component, ComponentStartup args)
    {
    }
}
