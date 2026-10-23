using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Shared.EntityEffects.Effects.Construction;

/// <summary>
/// Emptys all containers.
/// </summary>
public sealed partial class EmptyAllContainersEntityEffectSystem : EntityEffectSystem<TransformComponent, EmptyAllContainers>
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<EmptyAllContainers> args)
    {
        if (!HasComp<ContainerManagerComponent>(entity))
            return;

        foreach (var container in _container.GetAllContainers(entity))
        {
            foreach (var ent in _container.EmptyContainer(container, true, reparent: !args.Effect.Pickup))
            {
                if (args.Effect.EmptyAtUser && args.User is not null)
                    _transform.DropNextTo(ent, entity.Owner);

                if (args.Effect.Pickup)
                    _hands.PickupOrDrop(args.User, ent);
            }
        }
    }
}

public sealed partial class EmptyAllContainers : EntityEffectBase<EmptyAllContainers>
{
    /// <summary>
    /// Whether the user should attempt to pick up the removed entities.
    /// </summary>
    [DataField]
    public bool Pickup;

    /// <summary>
    /// Whether to empty the container at the user's location.
    /// </summary>
    [DataField]
    public bool EmptyAtUser;
}
