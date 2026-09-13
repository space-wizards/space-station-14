using Robust.Shared.Containers;

namespace Content.Shared.EntityEffects.Effects.Storage;

/// <summary>
/// Drops all items from all of the entity's containers.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class EmptyAllContainersEntityEffectSystem : EntityEffectSystem<ContainerManagerComponent, EmptyAllContainers>
{
    [Dependency] private SharedContainerSystem _container = default!;

    protected override void Effect(Entity<ContainerManagerComponent> entity, ref EntityEffectEvent<EmptyAllContainers> args)
    {
        foreach (var container in _container.GetAllContainers(entity, entity.Comp))
        {
            _container.EmptyContainer(container, true, Transform(entity).Coordinates);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class EmptyAllContainers : EntityEffectBase<EmptyAllContainers>;
