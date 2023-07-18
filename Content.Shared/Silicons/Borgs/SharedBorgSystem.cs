using Content.Shared.Body.Systems;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Borgs;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedBorgSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BorgChassisComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, BorgChassisComponent component, ComponentStartup args)
    {
        component.BrainContainer = _container.EnsureContainer<ContainerSlot>(uid, component.BrainContainerId);
    }
}
