using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Containers;

namespace Content.Shared.Trigger.Systems;

public sealed class EmptyContainersOnTriggerSystem : XOnTriggerSystem<EmptyContainersOnTriggerComponent>
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    protected override void OnTrigger(Entity<EmptyContainersOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!TryComp<ContainerManagerComponent>(target, out var containerComp))
            return;

        if (ent.Comp.Container is null)
        {
            foreach (var container in _container.GetAllContainers(target, containerComp))
            {
                _container.EmptyContainer(container);
            }
        }

        else
        {
            foreach (var containerId in ent.Comp.Container)
            {
                if (!_container.TryGetContainer(target, containerId, out var container, containerComp))
                    continue;

                _container.EmptyContainer(container);
            }
        }
    }
}
