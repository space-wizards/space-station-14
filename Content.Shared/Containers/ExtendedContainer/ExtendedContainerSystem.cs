using System.Linq;
using Content.Shared.Destructible;
using Robust.Shared.Containers;

namespace Content.Shared.Containers.ExtendedContainer;

public sealed class ExtendedContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtendedContainerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ExtendedContainerComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<ExtendedContainerComponent, DestructionEventArgs>(OnBreak);
        SubscribeLocalEvent<ExtendedContainerComponent, ContainerIsInsertingAttemptEvent>(OnContainerIsInsertingAttempt);
        SubscribeLocalEvent<ExtendedContainerComponent, ContainerIsRemovingAttemptEvent>(OnContainerIsRemovingAttempt);
    }

    /// <summary>
    /// Ensure the containers.
    /// </summary>
    private void OnComponentInit(EntityUid uid, ExtendedContainerComponent component, ComponentInit args)
    {
        foreach (var (id, container) in component.Containers)
        {
            container.BaseContainer = _containers.EnsureContainer<Container>(uid, id);
        }
    }

    private void OnBreak(EntityUid uid, ExtendedContainerComponent component, EntityEventArgs args)
    {
        foreach (var container in component.Containers.Values)
        {
            if (container.DeleteContentsOnBreak)
                return;

            if (container.BaseContainer == null)
                return;

            foreach (var entity in container.BaseContainer.ContainedEntities.ToArray())
            {
                container.BaseContainer.Remove(entity);
            }
        }
    }

    /// <summary>
    /// Cancel removal from the container if the removed entity is not part of the whitelist then play a sound
    /// </summary>
    private void OnContainerIsRemovingAttempt(EntityUid uid, ExtendedContainerComponent component, ContainerIsRemovingAttemptEvent args)
    {
        if (!component.Containers.TryGetValue(args.Container.ID, out var container))
            return;

        if (container.RemoveWhitelist != null &&
            container.RemoveWhitelist.IsValid(args.EntityUid, EntityManager))
        {
            args.Cancel();
            return;
        }

        if (container.RemoveSound != null)
            _audioSystem.PlayPredicted(container.RemoveSound, Transform(uid).Coordinates, uid);
    }

    /// <summary>
    /// Cancel insertion into the container if the inserting entity does not pass the whitelist
    /// or the container is full, play a sound if successful
    /// </summary>
    private void OnContainerIsInsertingAttempt(EntityUid uid, ExtendedContainerComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!component.Containers.TryGetValue(args.Container.ID, out var container))
            return;

        var isContainerFull = container.BaseContainer?.ContainedEntities.Count >= container.Capacity;

        if (isContainerFull ||
            container.InsertWhitelist != null &&
            container.InsertWhitelist.IsValid(args.EntityUid, EntityManager))
        {
            args.Cancel();
            return;
        }

        if (container.InsertSound != null)
            _audioSystem.PlayPredicted(container.InsertSound, Transform(uid).Coordinates, uid);
    }
}
