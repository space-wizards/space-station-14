using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Bed.Cryostorage;

/// <summary>
/// This handles <see cref="CryostorageComponent"/>
/// </summary>
public abstract class SharedCryostorageSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedMindSystem Mind = default!;

    protected bool CryostorageMapEnabled = false;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CryostorageComponent, EntInsertedIntoContainerMessage>(OnInsertedContainer);
        SubscribeLocalEvent<CryostorageComponent, EntRemovedFromContainerMessage>(OnRemovedContainer);
        SubscribeLocalEvent<CryostorageComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);

        SubscribeLocalEvent<CryostorageContainedComponent, EntGotRemovedFromContainerMessage>(OnRemovedContained);
    }

    private void OnInsertedContainer(Entity<CryostorageComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var (_, comp) = ent;
        if (args.Container.ID != comp.ContainerId)
            return;

        _appearance.SetData(ent, CryostorageVisuals.Full, true);
        var containedComp = EnsureComp<CryostorageContainedComponent>(args.Entity);
        containedComp.GracePeriodEndTime = Timing.CurTime + comp.GracePeriod;
    }

    private void OnRemovedContainer(Entity<CryostorageComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        var (_, comp) = ent;
        if (args.Container.ID != comp.ContainerId)
            return;

        _appearance.SetData(ent, CryostorageVisuals.Full, args.Container.ContainedEntities.Count > 0);
    }

    private void OnInsertAttempt(Entity<CryostorageComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        var (_, comp) = ent;
        if (args.Container.ID != comp.ContainerId)
            return;

        if (!TryComp<MindContainerComponent>(args.EntityUid, out var mindContainer))
        {
            args.Cancel();
            return;
        }

        if (Mind.TryGetMind(args.EntityUid, out _, out var mindComp, mindContainer) &&
            (mindComp.PreventSuicide || mindComp.PreventGhosting))
        {
            args.Cancel();
        }
    }

    private void OnRemovedContained(Entity<CryostorageContainedComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        var (_, comp) = ent;
        if (!comp.StoredOnMap)
            RemCompDeferred(ent, comp);
    }
}
