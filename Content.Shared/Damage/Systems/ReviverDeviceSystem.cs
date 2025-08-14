using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Damage.Systems;

/// <summary>
/// System for restoring damaged entities to full health using a <see cref="ReviverDeviceComponent"/>.
/// The entities to be restored must have a <see cref="DamageableComponent"/> and <see cref="MobStateComponent"/>.
/// </summary>
public sealed partial class ReviverDeviceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReviverDeviceComponent, InitiateReviverDeviceRestorationEvent>(OnInitiateRepair);
        SubscribeLocalEvent<ReviverDeviceComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ReviverDeviceComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<ReviverDeviceComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<ReviverDeviceComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        StopRestoration(ent);
    }

    private void OnExamine(Entity<ReviverDeviceComponent> ent, ref ExaminedEvent args)
    {

    }

    private void OnRemoved(Entity<ReviverDeviceComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        StopRestoration(ent);
    }

    private void OnInserted(Entity<ReviverDeviceComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateAppearance(ent);

        StartRestoration(ent);
    }

    private void OnInitiateRepair(Entity<ReviverDeviceComponent> ent, ref InitiateReviverDeviceRestorationEvent ev)
    {
        StartRestoration(ent);
    }

    /// <summary>
    /// Initiates the restoration of a damaged entity contained in the specified restoring entity.
    /// </summary>
    /// <param name="ent">The restoring entity.</param>
    private void StartRestoration(Entity<ReviverDeviceComponent> ent)
    {
        if (ent.Comp.RestorationInProgress)
            return;

        if (!TryGetRestorationTarget(ent, out var target))
            return;

        ent.Comp.RestorationInProgress = true;
        ent.Comp.RestorationStartTime = _timing.CurTime;
        ent.Comp.RestorationEndTime = CalculateRecoveryEndTime(ent, target.Value);
        Dirty(ent);

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Updates the current restoration of any damaged AIs contained in the specified restoring entity.
    /// </summary>
    /// <param name="ent">The restoring entity.</param>
    private void UpdateRestoration(Entity<ReviverDeviceComponent> ent)
    {
        if (!ent.Comp.RestorationInProgress)
            return;

        if (!TryGetRestorationTarget(ent, out var target))
        {
            StopRestoration(ent);
            return;
        }

        if (_timing.CurTime >= ent.Comp.RestorationEndTime)
        {
            FinalizeRestoration(ent);
            return;
        }

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Terminates the restoration of any damaged AIs contained in the specified restoring entity.
    /// </summary>
    /// <param name="ent">The restoring entity.</param>
    private void StopRestoration(Entity<ReviverDeviceComponent> ent)
    {
        if (!ent.Comp.RestorationInProgress)
            return;

        ent.Comp.RestorationInProgress = false;
        Dirty(ent);

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Finalizes the restoration of any damaged AIs contained in the specified restoring entity,
    /// removing all damage and turning them to life.
    /// </summary>
    /// <param name="ent">The restoring entity.</param>
    private void FinalizeRestoration(Entity<ReviverDeviceComponent> ent)
    {
        if (!ent.Comp.RestorationInProgress)
            return;

        if (!TryGetRestorationTarget(ent, out var target))
        {
            StopRestoration(ent);
            return;
        }

        if (TryComp<DamageableComponent>(target, out var targetDamageable))
        {
            _damageable.SetAllDamage(target.Value, targetDamageable, 0);
        }

        if (ent.Comp.ResurrectTarget && TryComp<MobStateComponent>(target, out var targetMobState))
        {
            _mobState.ChangeMobState(target.Value, MobState.Alive);
        }

        StopRestoration(ent);
    }

    /// <summary>
    /// Updates the appearance of the specified restoring entity based on its current state.
    /// </summary>
    /// <param name="ent">The restoring entity.</param>
    private void UpdateAppearance(Entity<ReviverDeviceComponent> ent)
    {
        if (!TryComp<AppearanceComponent>(ent, out var entAppearance))
            return;

        _appearance.RemoveData(ent, ReviverDeviceVisuals.MobState);
        _appearance.RemoveData(ent, ReviverDeviceVisuals.RestorationProgress);

        if (!TryGetRestorationTarget(ent, out var target) ||
            !TryComp<MobStateComponent>(target, out var mobState))
        {
            _appearance.SetData(ent, ReviverDeviceVisuals.MobState, MobState.Invalid);
            return;
        }

        if (ent.Comp.RestorationInProgress)
        {
            var stage = CalculateRecoveryStage(ent);
            _appearance.SetData(ent, ReviverDeviceVisuals.RestorationProgress, stage);
            return;
        }

        _appearance.SetData(ent, ReviverDeviceVisuals.RestorationProgress, mobState.CurrentState);
    }

    /// <summary>
    /// Calculates the time at which the target entity will be restored to full health.
    /// </summary>
    /// <param name="ent">The restoring entity.</param>
    /// <param name="target">The target entity.</param>
    /// <returns>TimeSpan in seconds.</returns>
    private TimeSpan CalculateRecoveryEndTime(Entity<ReviverDeviceComponent> ent, EntityUid target)
    {
        if (TryComp<DamageableComponent>(target, out var targetDamageable))
        {
            var duration = TimeSpan.FromSeconds((float)targetDamageable.TotalDamage / ent.Comp.RestorationRate);
            return _timing.CurTime + duration;
        }

        return _timing.CurTime;
    }

    /// <summary>
    /// Calculates the current stage of any in-progress restorations.
    /// </summary>
    /// <param name="ent">The restoring entity.</param>
    /// <returns>The current stage.</returns>
    private int CalculateRecoveryStage(Entity<ReviverDeviceComponent> ent)
    {
        var completionPercentage = (_timing.CurTime - ent.Comp.RestorationStartTime) / (ent.Comp.RestorationEndTime - ent.Comp.RestorationStartTime);

        return (int)(completionPercentage * ent.Comp.RestorationStageCount);
    }

    /// <summary>
    /// Gets a valid target for restoration which is currently contained inside the specified restoring entity.
    /// </summary>
    /// <param name="ent">The restoring entity.</param>
    /// <param name="target">The found target.</param>
    /// <returns>True if a valid target was found.</returns>
    private bool TryGetRestorationTarget(Entity<ReviverDeviceComponent> ent, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;

        if (!_container.TryGetContainer(ent, ent.Comp.RestorationContainer, out var container))
            return false;

        if (container.Count == 0)
            return false;

        target = container.ContainedEntities[0];

        if (!HasComp<DamageableComponent>(target) || !HasComp<MobStateComponent>(target))
            return false;

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<ReviverDeviceComponent>();

        while (query.MoveNext(out var uid, out var stationAiRestorer))
        {
            if (!stationAiRestorer.RestorationInProgress)
                continue;

            UpdateRestoration((uid, stationAiRestorer));
        }
    }
}
