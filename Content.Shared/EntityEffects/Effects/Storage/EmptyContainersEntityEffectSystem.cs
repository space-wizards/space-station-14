using Robust.Shared.Containers;

namespace Content.Shared.EntityEffects.Effects.Storage;

/// <summary>
/// Drops all items from specified containers of the entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class EmptyContainersEntityEffectSystem : EntityEffectSystem<ContainerManagerComponent, EmptyContainers>
{
    [Dependency] private SharedContainerSystem _container = default!;

    protected override void Effect(Entity<ContainerManagerComponent> entity, ref EntityEffectEvent<EmptyContainers> args)
    {
        foreach (var containerId in args.Effect.Containers)
        {
            if (!_container.TryGetContainer(entity, containerId, out var container, entity.Comp))
                continue;

            _container.EmptyContainer(container, true);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class EmptyContainers : EntityEffectBase<EmptyContainers>
{
    /// <summary>
    /// The IDs of the containers to empty.
    /// </summary>
    [DataField]
    public List<string> Containers = [];
}
