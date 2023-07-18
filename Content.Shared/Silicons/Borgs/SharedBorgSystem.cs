using Content.Shared.Body.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Borgs;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedBorgSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BorgChassisComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, BorgChassisComponent component, ComponentStartup args)
    {
        component.BrainContainer = Container.EnsureContainer<ContainerSlot>(uid, component.BrainContainerId);
    }
}
