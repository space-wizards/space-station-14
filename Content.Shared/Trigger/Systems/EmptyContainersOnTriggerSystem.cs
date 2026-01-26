using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Containers;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// Empty containers trigger system.
/// </summary>
public sealed class EmptyContainersOnTriggerSystem : XOnTriggerSystem<EmptyContainersOnTriggerComponent>
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    protected override void OnTrigger(Entity<EmptyContainersOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!TryComp<ContainerManagerComponent>(target, out var containerComp))
            return;

        // Empty everything. Make sure a player isn't the target because they will get removed from their body along with their organs
        if (ent.Comp.Container is null)
        {
            foreach (var container in _container.GetAllContainers(target, containerComp))
            {
                _container.EmptyContainer(container);
            }

            args.Handled = true;
        }

        // Empty containers in a sane way
        else
        {
            foreach (var containerId in ent.Comp.Container)
            {
                if (!_container.TryGetContainer(target, containerId, out var container, containerComp))
                    continue;

                _container.EmptyContainer(container);
                args.Handled = true;
            }
        }
    }
}

/// <summary>
/// Empty containers and delete items trigger system.
/// </summary>
public sealed class CleanContainersOnTriggerSystem : XOnTriggerSystem<CleanContainersOnTriggerComponent>
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    protected override void OnTrigger(Entity<CleanContainersOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!TryComp<ContainerManagerComponent>(target, out var containerComp))
            return;

        // Empty everything. Make sure a player isn't the target because they will get DELETED
        if (ent.Comp.Container is null)
        {
            foreach (var container in _container.GetAllContainers(target, containerComp))
            {
                _container.CleanContainer(container);
            }

            args.Handled = true;
        }

        // Empty containers in a sane way
        else
        {
            foreach (var containerId in ent.Comp.Container)
            {
                if (!_container.TryGetContainer(target, containerId, out var container, containerComp))
                    continue;

                _container.CleanContainer(container);
                args.Handled = true;
            }
        }
    }
}

