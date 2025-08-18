using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Lock;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// This system is used to handle the actions of AI Restoration Consoles.
/// These consoles can be used to repair damaged station AIs,
/// or destroy them utterly.
/// </summary>
public sealed partial class StationAiFixerConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSuicideSystem _suicide = default!;

    private readonly string _intellicardHolder = "intellicard_holder";
    private readonly string _stationAiMindSlot = "station_ai_mind_slot";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiFixerConsoleComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, LockToggledEvent>(OnLockToggle);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, StationAiFixerConsoleMessage>(OnMessage);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInserted(Entity<StationAiFixerConsoleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        /*
        // DEBUGGING
        if (TryGetRestorationTarget(ent, out var target))
        {
            if (TryComp<DamageableComponent>(target, out var targetDamageable))
                _damageable.SetAllDamage(target.Value, targetDamageable, 25);

            if (TryComp<MobStateComponent>(target, out var targetMobState))
                _mobState.ChangeMobState(target.Value, MobState.Dead);
        }
        // DEBUGGING
        */

        if (_userInterface.TryGetOpenUi(ent.Owner, StationAiFixerConsoleUiKey.Key, out var bui))
            bui.Update<StationAiFixerConsoleBoundUserInterfaceState>();

        UpdateAppearance(ent);
    }

    private void OnRemoved(Entity<StationAiFixerConsoleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_userInterface.TryGetOpenUi(ent.Owner, StationAiFixerConsoleUiKey.Key, out var bui))
            bui.Update<StationAiFixerConsoleBoundUserInterfaceState>();

        StopAction(ent);
    }

    private void OnLockToggle(Entity<StationAiFixerConsoleComponent> ent, ref LockToggledEvent args)
    {
        if (_userInterface.TryGetOpenUi(ent.Owner, StationAiFixerConsoleUiKey.Key, out var bui))
            bui.Update<StationAiFixerConsoleBoundUserInterfaceState>();
    }

    private void OnMessage(Entity<StationAiFixerConsoleComponent> ent, ref StationAiFixerConsoleMessage args)
    {
        if (TryComp<LockComponent>(ent, out var lockable) && lockable.Locked)
            return;

        switch (args.Action)
        {
            case StationAiFixerConsoleAction.Eject:
                EjectIntellicard(ent, args.Actor);
                break;
            case StationAiFixerConsoleAction.Repair:
                RepairStationAi(ent, args.Actor);
                break;
            case StationAiFixerConsoleAction.Purge:
                PurgeStationAi(ent, args.Actor);
                break;
            case StationAiFixerConsoleAction.Cancel:
                CancelRestoration(ent, args.Actor);
                break;
        }
    }

    private void OnPowerChanged(Entity<StationAiFixerConsoleComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        StopAction(ent);
    }

    private void OnExamined(Entity<StationAiFixerConsoleComponent> ent, ref ExaminedEvent args)
    {
        var message = IsIntellicardInserted(ent) ?
            "station-ai-fixer-console-examination-intellicard-present" :
            "station-ai-fixer-console-examination-intellicard-absent";

        args.PushMarkup(Loc.GetString(message));
    }

    private void EjectIntellicard(Entity<StationAiFixerConsoleComponent> ent, EntityUid user)
    {
        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;

        if (!_itemSlots.TryGetSlot(ent, _intellicardHolder, out var intellicardSlot, slots))
            return;

        _itemSlots.TryEjectToHands(ent, intellicardSlot, user, true);
    }

    private void RepairStationAi(Entity<StationAiFixerConsoleComponent> ent, EntityUid user)
    {
        if (!TryGetTarget(ent, out var target))
            return;

        StartAction(ent, target.Value, StationAiFixerConsoleAction.Repair);
    }

    private void PurgeStationAi(Entity<StationAiFixerConsoleComponent> ent, EntityUid user)
    {
        if (!TryGetTarget(ent, out var target))
            return;

        StartAction(ent, target.Value, StationAiFixerConsoleAction.Purge);
    }

    private void CancelRestoration(Entity<StationAiFixerConsoleComponent> ent, EntityUid user)
    {
        StopAction(ent);
    }

    /// <summary>
    /// Initiates an action upon a target entity by the specified console.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <param name="target">The target entity.</param>
    /// <param name="action">The action to be enacted on the target.</param>
    private void StartAction(Entity<StationAiFixerConsoleComponent> ent, EntityUid target, StationAiFixerConsoleAction actionType)
    {
        if (IsActionInProgress(ent))
        {
            StopAction(ent);
        }

        if (IsTargetValid(ent, target, actionType))
        {
            ent.Comp.ActionType = actionType;
            ent.Comp.ActionTarget = target;
            ent.Comp.ActionStartTime = _timing.CurTime;
            ent.Comp.ActionEndTime = CalculateActionEndTime(ent, target, actionType);
            ent.Comp.CurrentActionStage = 0;
            Dirty(ent);
        }

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Updates the current action being conducted by the specified console.
    /// </summary>
    /// <param name="ent">The console.</param>
    private void UpdateAction(Entity<StationAiFixerConsoleComponent> ent)
    {
        if (IsActionInProgress(ent))
        {
            if (ent.Comp.ActionTarget == null)
            {
                StopAction(ent);
                return;
            }

            if (_timing.CurTime >= ent.Comp.ActionEndTime)
            {
                FinalizeAction(ent);
                return;
            }

            var currentStage = CalculateActionStage(ent);

            if (currentStage != ent.Comp.CurrentActionStage)
            {
                ent.Comp.CurrentActionStage = currentStage;
                Dirty(ent);
            }
        }

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Terminates any action being conducted by the specified console.
    /// </summary>
    /// <param name="ent">The console.</param>
    private void StopAction(Entity<StationAiFixerConsoleComponent> ent)
    {
        ent.Comp.ActionType = StationAiFixerConsoleAction.None;
        ent.Comp.ActionTarget = null;
        Dirty(ent);

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Finalizes the action being conducted by the specified console
    /// (e.g., repairing a target).
    /// </summary>
    /// <param name="ent">The console.</param>
    private void FinalizeAction(Entity<StationAiFixerConsoleComponent> ent)
    {
        if (IsActionInProgress(ent))
        {
            if (ent.Comp.ActionType == StationAiFixerConsoleAction.Repair)
            {
                if (TryComp<DamageableComponent>(ent.Comp.ActionTarget, out var targetDamageable))
                {
                    _damageable.SetAllDamage(ent.Comp.ActionTarget.Value, targetDamageable, 0);
                }

                if (TryComp<MobStateComponent>(ent.Comp.ActionTarget, out var targetMobState))
                {
                    _mobState.ChangeMobState(ent.Comp.ActionTarget.Value, MobState.Alive);
                }
            }
            else if (ent.Comp.ActionType == StationAiFixerConsoleAction.Purge)
            {
                QueueDel(ent.Comp.ActionTarget);
            }
        }

        StopAction(ent);
    }

    /// <summary>
    /// Updates the appearance of the specified console based on its current state.
    /// </summary>
    /// <param name="ent">The console.</param>
    private void UpdateAppearance(Entity<StationAiFixerConsoleComponent> ent)
    {
        if (!TryComp<AppearanceComponent>(ent, out var entAppearance))
            return;

        if (IsActionInProgress(ent))
        {
            if (!_appearance.TryGetData(ent, StationAiFixerConsoleVisuals.ActionProgress, out int oldStage) ||
                oldStage != ent.Comp.CurrentActionStage)
            {
                _appearance.RemoveData(ent, StationAiFixerConsoleVisuals.MobState);
                _appearance.SetData(ent, StationAiFixerConsoleVisuals.ActionProgress, ent.Comp.CurrentActionStage);

                return;
            }
        }

        var target = ent.Comp.ActionTarget;

        if (target == null)
        {
            TryGetTarget(ent, out target);
        }

        var currentMobState = MobState.Invalid;

        if (TryComp<MobStateComponent>(target, out var mobState))
        {
            currentMobState = mobState.CurrentState;
        }

        _appearance.RemoveData(ent, StationAiFixerConsoleVisuals.ActionProgress);
        _appearance.SetData(ent, StationAiFixerConsoleVisuals.MobState, currentMobState);
    }

    /// <summary>
    /// Calculates the time at which a specified console action will conclude.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <param name="target">The target entity.</param>
    /// <param name="actionType">The action to be enacted on the target.</param>
    /// <returns>TimeSpan in seconds.</returns>
    private TimeSpan CalculateActionEndTime(Entity<StationAiFixerConsoleComponent> ent, EntityUid target, StationAiFixerConsoleAction actionType)
    {
        if (TryComp<DamageableComponent>(target, out var targetDamageable))
        {
            var duration = ent.Comp.ActionTimeLimits.Y;

            if (actionType == StationAiFixerConsoleAction.Repair &&
                ent.Comp.RepairRate > 0)
            {
                duration = (float)targetDamageable.TotalDamage / ent.Comp.RepairRate;
            }
            else if (actionType == StationAiFixerConsoleAction.Purge &&
                ent.Comp.PurgeRate > 0 &&
                TryComp<MobThresholdsComponent>(target, out var mobThresholds))
            {
                // If the target does not have a MobThresholdsComponent, it will still be purged,
                // but it will take the maximum amount of time to do so.
                var lethalDamage = mobThresholds.Thresholds.Keys.Last() - targetDamageable.TotalDamage;
                duration = (float)lethalDamage / ent.Comp.PurgeRate;
            }

            duration = Math.Clamp(duration, ent.Comp.ActionTimeLimits.X, ent.Comp.ActionTimeLimits.Y);
            return _timing.CurTime + TimeSpan.FromSeconds(duration);
        }

        return _timing.CurTime;
    }

    /// <summary>
    /// Calculates the current stage of any in-progress actions.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <returns>The current stage.</returns>
    private int CalculateActionStage(Entity<StationAiFixerConsoleComponent> ent)
    {
        var completionPercentage = (_timing.CurTime - ent.Comp.ActionStartTime) / (ent.Comp.ActionEndTime - ent.Comp.ActionStartTime);

        return (int)(completionPercentage * ent.Comp.ActionStageCount);
    }

    /// <summary>
    /// Try to find a valid target being stored inside the specified console.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <param name="target">The found target.</param>
    /// <returns>True, if a valid target was found.</returns>
    public bool TryGetTarget(Entity<StationAiFixerConsoleComponent> ent, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;

        if (!_container.TryGetContainer(ent, _intellicardHolder, out var intellicardHolder) || intellicardHolder.Count == 0)
            return false;

        var intellicard = intellicardHolder.ContainedEntities[0];

        if (!_container.TryGetContainer(intellicard, _stationAiMindSlot, out var stationAiMindSlot) || stationAiMindSlot.Count == 0)
            return false;

        var stationAi = stationAiMindSlot.ContainedEntities[0];

        if (!HasComp<DamageableComponent>(stationAi) || !HasComp<MobStateComponent>(stationAi))
            return false;

        target = stationAi;

        return true;
    }

    /// <summary>
    /// Determines if a target entity can be acted upon by the specified console.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <param name="target">The target.</param>
    /// <param name="actionType">The action to be enacted on the target.</param>
    /// <returns>True, if the target is valid for the specified console action.</returns>
    private bool IsTargetValid(Entity<StationAiFixerConsoleComponent> ent, EntityUid target, StationAiFixerConsoleAction actionType)
    {
        if (actionType == StationAiFixerConsoleAction.Purge)
            return true;

        if (actionType == StationAiFixerConsoleAction.Repair)
        {
            if (TryComp<DamageableComponent>(target, out var targetDamageable) &&
                targetDamageable.TotalDamage > 0)
            {
                return true;
            }

            if (TryComp<MobStateComponent>(target, out var targetMobState) &&
                targetMobState.CurrentState == MobState.Dead)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns whether an intellicard is inserted into the specified console.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <returns>True, if an intellicard is inserted.</returns>
    public bool IsIntellicardInserted(Entity<StationAiFixerConsoleComponent> ent)
    {
        return _container.TryGetContainer(ent, _intellicardHolder, out var intellicardHolder) && intellicardHolder.Count > 0;
    }

    /// <summary>
    /// Returns whether the specified console has an action in progress.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <returns>Ture, if an action is in progress.</returns>
    private bool IsActionInProgress(Entity<StationAiFixerConsoleComponent> ent)
    {
        return ent.Comp.ActionType != StationAiFixerConsoleAction.None;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<StationAiFixerConsoleComponent>();

        while (query.MoveNext(out var uid, out var stationAiFixerConsole))
        {
            var ent = (uid, stationAiFixerConsole);

            if (!IsActionInProgress(ent))
                continue;

            UpdateAction(ent);
        }
    }
}
