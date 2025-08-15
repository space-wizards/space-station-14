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

    private void OnRemoved(Entity<ReviverDeviceComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        StopRestoration(ent);
    }

    private void OnInserted(Entity<ReviverDeviceComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateAppearance(ent);

        /*
        // DEBUGGING
        if (TryGetRestorationTarget(ent, out var target))
        {
            if (TryComp<DamageableComponent>(target, out var targetDamageable))
                _damageable.SetAllDamage(target.Value, targetDamageable, 25);

            if (TryComp<MobStateComponent>(target, out var targetMobState))
                _mobState.ChangeMobState(target.Value, MobState.Dead);
        }

        StartRestoration(ent);
        // DEBUGGING
        */
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
        if (!ent.Comp.RestorationInProgress &&
            TryGetRestorationTarget(ent, out var target) &&
            IsTargetValidForRestoration(ent, target.Value))
        {
            ent.Comp.RestorationInProgress = true;
            ent.Comp.RestorationStartTime = _timing.CurTime;
            ent.Comp.RestorationEndTime = CalculateRecoveryEndTime(ent, target.Value);
            Dirty(ent);
        }

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Updates the current restoration of any damaged AIs contained in the specified restoring entity.
    /// </summary>
    /// <param name="ent">The restoring entity.</param>
    private void UpdateRestoration(Entity<ReviverDeviceComponent> ent)
    {
        if (ent.Comp.RestorationInProgress)
        {
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
        }

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Terminates the restoration of any damaged AIs contained in the specified restoring entity.
    /// </summary>
    /// <param name="ent">The restoring entity.</param>
    private void StopRestoration(Entity<ReviverDeviceComponent> ent)
    {
        if (ent.Comp.RestorationInProgress)
        {
            ent.Comp.RestorationInProgress = false;
            Dirty(ent);
        }

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Finalizes the restoration of any damaged AIs contained in the specified restoring entity,
    /// removing all damage and turning them to life.
    /// </summary>
    /// <param name="ent">The restoring entity.</param>
    private void FinalizeRestoration(Entity<ReviverDeviceComponent> ent)
    {
        if (ent.Comp.RestorationInProgress &&
            TryGetRestorationTarget(ent, out var target))
        {
            if (TryComp<DamageableComponent>(target, out var targetDamageable))
            {
                _damageable.SetAllDamage(target.Value, targetDamageable, 0);
            }

            if (ent.Comp.ResurrectTarget && TryComp<MobStateComponent>(target, out var targetMobState))
            {
                _mobState.ChangeMobState(target.Value, MobState.Alive);
            }
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

        if (ent.Comp.RestorationInProgress)
        {
            var currentStage = CalculateRecoveryStage(ent);

            if (!_appearance.TryGetData(ent, ReviverDeviceVisuals.RestorationProgress, out int oldStage) ||
                oldStage != currentStage)
            {
                _appearance.RemoveData(ent, ReviverDeviceVisuals.MobState);
                _appearance.SetData(ent, ReviverDeviceVisuals.RestorationProgress, currentStage);
            }

            return;
        }

        var currentMobState = MobState.Invalid;

        if (TryGetRestorationTarget(ent, out var target) &&
            TryComp<MobStateComponent>(target, out var mobState))
        {
            currentMobState = mobState.CurrentState;
        }

        if (!_appearance.TryGetData(ent, ReviverDeviceVisuals.MobState, out MobState oldMobState) ||
                oldMobState != currentMobState)
        {
            _appearance.RemoveData(ent, ReviverDeviceVisuals.RestorationProgress);
            _appearance.SetData(ent, ReviverDeviceVisuals.MobState, currentMobState);
        }
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

        if (!_container.TryGetContainer(ent, ent.Comp.RestorationContainer, out var restorationContainer))
            return false;

        if (restorationContainer.Count == 0)
            return false;

        foreach (var potentialTarget in restorationContainer.ContainedEntities)
        {
            if (HasComp<DamageableComponent>(potentialTarget) &&
                HasComp<MobStateComponent>(potentialTarget))
            {
                target = potentialTarget;
                return true;
            }

            foreach (var targetContainerName in ent.Comp.TargetContainers)
            {
                if (!_container.TryGetContainer(potentialTarget, targetContainerName, out var targetContainer) ||
                    targetContainer.Count == 0)
                {
                    continue;
                }

                foreach (var otherPotentialTarget in targetContainer.ContainedEntities)
                {
                    if (HasComp<DamageableComponent>(otherPotentialTarget) &&
                        HasComp<MobStateComponent>(otherPotentialTarget))
                    {
                        target = otherPotentialTarget;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsTargetValidForRestoration(Entity<ReviverDeviceComponent> ent, EntityUid target)
    {
        if (TryComp<DamageableComponent>(target, out var targetDamageable) &&
            targetDamageable.TotalDamage > 0)
        {
            return true;
        }

        if (ent.Comp.ResurrectTarget &&
            TryComp<MobStateComponent>(target, out var targetMobState) &&
            targetMobState.CurrentState == MobState.Dead)
        {
            return true;
        }

        return false;
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
