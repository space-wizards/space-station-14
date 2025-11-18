using Content.Shared.Climbing.Systems;
using Content.Shared.Movement.Events;
using Robust.Shared.Containers;

namespace Content.Shared.Containers;

public sealed class ExitContainerOnMoveSystem : EntitySystem
{
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExitContainerOnMoveComponent, ContainerRelayMovementEntityEvent>(OnContainerRelay);
    }

    private void OnContainerRelay(Entity<ExitContainerOnMoveComponent> ent, ref ContainerRelayMovementEntityEvent args)
    {
        var (_, comp) = ent;
        if (!TryComp<ContainerManagerComponent>(ent, out var containerManager))
            return;

        if (!_container.TryGetContainer(ent, comp.ContainerId, out var container, containerManager) || !container.Contains(args.Entity))
            return;

        _climb.ForciblySetClimbing(args.Entity, ent);
        _container.RemoveEntity(ent, args.Entity, containerManager);
    }
}
