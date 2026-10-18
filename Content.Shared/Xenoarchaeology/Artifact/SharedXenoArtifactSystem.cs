using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact;

/// <summary>
/// Handles all logic for generating and facilitating interactions with XenoArtifacts
/// </summary>
public abstract partial class SharedXenoArtifactSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] protected IRobustRandom RobustRandom = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoArtifactComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<XenoArtifactComponent, ArtifactSelfActivateEvent>(OnSelfActivate);

        InitializeNode();
        InitializeUnlock();
        InitializeXAT();
        InitializeXAE();
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateUnlock(frameTime);
    }

    /// <summary> Clears all attached scanners.  </summary>
    [SubscribeLocalEvent]
    private void OnShutdown(Entity<XenoArtifactComponent> ent, ref ComponentRemove shutdown)
    {
        var removedEvent = new XenoArtifactDestroyedEvent();
        foreach (var entity in ent.Comp.AttachedEntities)
        {
            RaiseLocalEvent(entity, ref removedEvent);
        }
    }

    /// <summary> As all artifacts have to contain nodes - we ensure that they are containers. </summary>
    private void OnStartup(Entity<XenoArtifactComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ent.Comp.SelfActivateAction);
        ent.Comp.NodeContainer = _container.EnsureContainer<Container>(ent, XenoArtifactComponent.NodeContainerId);
    }

    private void OnSelfActivate(Entity<XenoArtifactComponent> ent, ref ArtifactSelfActivateEvent args)
    {
        args.Handled = TryActivateXenoArtifact(ent, ent, null, Transform(ent).Coordinates, false);
    }

    /// <summary>
    /// Tries to remove an entity from the list of attached entities.
    /// This helps with tracking relationship.
    /// </summary>
    /// <param name="ent">Artifact entity.</param>
    /// <param name="entityToDetach">Entity to detach.</param>
    /// <returns>
    /// Returns false if <paramref name="ent"/> is not artifact,
    /// or if there is no such entity in list of attached ones.
    /// Otherwise, returns true.
    /// </returns>
    public bool TryDetachEntity(Entity<XenoArtifactComponent?> ent, EntityUid entityToDetach)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var result = ent.Comp.AttachedEntities.Remove(entityToDetach);
        if (result)
            Dirty(ent);

        return result;
    }

    /// <summary>
    /// Tries to add an entity to list of attached entities.
    /// This helps with tracking relationship.
    /// </summary>
    /// <param name="ent">Artifact entity.</param>
    /// <param name="entityToAttach">Entity to attach.</param>
    /// <returns>
    /// Returns False if <paramref name="ent"/> is not artifact,
    /// or entity is already added, otherwise true.
    /// </returns>
    public bool TryAttachEntity(Entity<XenoArtifactComponent?> ent, EntityUid entityToAttach)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var result = ent.Comp.AttachedEntities.Add(entityToAttach);
        if (result)
            Dirty(ent);

        return result;
    }

    public void SetSuppressed(Entity<XenoArtifactComponent> ent, bool val)
    {
        if (ent.Comp.Suppressed == val)
            return;

        ent.Comp.Suppressed = val;
        Dirty(ent);
    }
}

/// <summary> Event of artifact destruction. </summary>
[ByRefEvent]
public record struct XenoArtifactDestroyedEvent;
