using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.Bed.Cryostorage;

/// <summary>
/// This handles <see cref="CryostorageComponent"/>
/// </summary>
public abstract class SharedCryostorageSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedMindSystem Mind = default!;

    protected EntityUid? PausedMap { get; private set; }

    protected bool CryoSleepRejoiningEnabled;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CryostorageComponent, EntInsertedIntoContainerMessage>(OnInsertedContainer);
        SubscribeLocalEvent<CryostorageComponent, EntRemovedFromContainerMessage>(OnRemovedContainer);
        SubscribeLocalEvent<CryostorageComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);

        SubscribeLocalEvent<CryostorageContainedComponent, EntGotRemovedFromContainerMessage>(OnRemovedContained);
        SubscribeLocalEvent<CryostorageContainedComponent, EntityUnpausedEvent>(OnUnpaused);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        _configuration.OnValueChanged(CCVars.GameCryoSleepRejoining, OnCvarChanged);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _configuration.UnsubValueChanged(CCVars.GameCryoSleepRejoining, OnCvarChanged);
    }

    private void OnCvarChanged(bool value)
    {
        if (CryoSleepRejoiningEnabled == value)
            return;

        CryoSleepRejoiningEnabled = value;

        if (value)
        {
            EnsurePausedMap();
        }
        else
        {
            DeletePausedMap();
        }
    }

    private void OnInsertedContainer(Entity<CryostorageComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var (_, comp) = ent;
        if (args.Container.ID != comp.ContainerId)
            return;

        _appearance.SetData(ent, CryostorageVisuals.Full, true);
        var containedComp = EnsureComp<CryostorageContainedComponent>(args.Entity);
        containedComp.GracePeriodEndTime = Timing.CurTime + comp.GracePeriod;
        containedComp.Cryostorage = ent;
        Dirty(args.Entity, containedComp);
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

    private void OnUnpaused(Entity<CryostorageContainedComponent> ent, ref EntityUnpausedEvent args)
    {
        if (ent.Comp.GracePeriodEndTime != null)
            ent.Comp.GracePeriodEndTime += args.PausedTime;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        DeletePausedMap();
    }

    private void DeletePausedMap()
    {
        if (PausedMap == null || !Exists(PausedMap))
            return;

        EntityManager.DeleteEntity(PausedMap.Value);
        PausedMap = null;
    }

    private void EnsurePausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return;

        var map = _mapManager.CreateMap();
        _mapManager.SetMapPaused(map, true);
        PausedMap = _mapManager.GetMapEntityId(map);
    }
}
