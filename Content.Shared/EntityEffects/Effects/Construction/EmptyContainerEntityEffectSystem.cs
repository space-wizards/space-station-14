using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Shared.EntityEffects.Effects.Construction;

/// <summary>
/// Emptys the container.
/// </summary>
public sealed partial class EmptyContainerEntityEffectSystem : EntityEffectSystem<TransformComponent, EmptyContainer>
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<EmptyContainer> args)
    {
        if (!TryComp<ContainerManagerComponent>(entity, out var containerManager)
            || !_container.TryGetContainer(entity, args.Effect.Container, out var container, containerManager))
            return;

        var hasHands = TryComp<HandsComponent>(args.User, out var hands);

        foreach (var ent in _container.EmptyContainer(container, true, reparent: !args.Effect.Pickup))
        {
            if (hasHands)
                _hands.PickupOrDrop(args.User, ent, dropNear: true, handsComp: hands);
        }
    }
}

public sealed partial class EmptyContainer : EntityEffectBase<EmptyContainer>
{
    [DataField]
    public string Container = string.Empty;

    /// <summary>
    /// Whether the user should attempt to pick up the removed entities.
    /// </summary>
    [DataField]
    public bool Pickup;
}
