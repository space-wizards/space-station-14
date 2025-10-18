using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Lock;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// This system is used to handle the actions of AI Restoration Consoles.
/// These consoles can be used to revive dead station AIs, or destroy them.
/// </summary>
public abstract partial class SharedStationAiFixerConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiFixerConsoleComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, LockToggledEvent>(OnLockToggle);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, StationAiFixerConsoleMessage>(OnMessage);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<StationAiCustomizationComponent, StationAiCustomizationStateChanged>(OnStationAiCustomizationStateChanged);
    }

    private void OnInserted(Entity<StationAiFixerConsoleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.StationAiHolderSlot)
            return;

        if (TryGetTarget(ent, out var target))
        {
            ent.Comp.ActionTarget = target;
            Dirty(ent);
        }

        UpdateAppearance(ent);
    }

    private void OnRemoved(Entity<StationAiFixerConsoleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.StationAiHolderSlot)
            return;

        ent.Comp.ActionTarget = null;

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
                EjectStationAiHolder(ent, args.Actor);
                break;
            case StationAiFixerConsoleAction.Repair:
                RepairStationAi(ent, args.Actor);
                break;
            case StationAiFixerConsoleAction.Purge:
                PurgeStationAi(ent, args.Actor);
                break;
            case StationAiFixerConsoleAction.Cancel:
                CancelAction(ent, args.Actor);
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
        var message = TryGetStationAiHolder(ent, out var holder) ?
            Loc.GetString("station-ai-fixer-console-examination-station-ai-holder-present", ("holder", Name(holder.Value))) :
            Loc.GetString("station-ai-fixer-console-examination-station-ai-holder-absent");

        args.PushMarkup(message);
    }

    private void OnStationAiCustomizationStateChanged(Entity<StationAiCustomizationComponent> ent, ref StationAiCustomizationStateChanged args)
    {
        if (_container.TryGetOuterContainer(ent, Transform(ent), out var outerContainer) &&
            TryComp<StationAiFixerConsoleComponent>(outerContainer.Owner, out var stationAiFixerConsole))
        {
            UpdateAppearance((outerContainer.Owner, stationAiFixerConsole));
        }
    }

    private void EjectStationAiHolder(Entity<StationAiFixerConsoleComponent> ent, EntityUid user)
    {
        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;

        if (!_itemSlots.TryGetSlot(ent, ent.Comp.StationAiHolderSlot, out var holderSlot, slots))
            return;

        if (_itemSlots.TryEjectToHands(ent, holderSlot, user, true))
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):user} ejected a station AI holder from AI restoration console ({ToPrettyString(ent.Owner)})");
    }

    private void RepairStationAi(Entity<StationAiFixerConsoleComponent> ent, EntityUid user)
    {
        if (ent.Comp.ActionTarget == null)
            return;

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):user} started a repair of {ToPrettyString(ent.Comp.ActionTarget)} using an AI restoration console ({ToPrettyString(ent.Owner)})");
        StartAction(ent, StationAiFixerConsoleAction.Repair);
    }

    private void PurgeStationAi(Entity<StationAiFixerConsoleComponent> ent, EntityUid user)
    {
        if (ent.Comp.ActionTarget == null)
            return;

        _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(user):user} started a purge of {ToPrettyString(ent.Comp.ActionTarget)} using {ToPrettyString(ent.Owner)}");
        StartAction(ent, StationAiFixerConsoleAction.Purge);
    }

    private void CancelAction(Entity<StationAiFixerConsoleComponent> ent, EntityUid user)
    {
        if (!IsActionInProgress(ent))
            return;

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):user} canceled operation involving {ToPrettyString(ent.Comp.ActionTarget)} and {ToPrettyString(ent.Owner)} ({ent.Comp.ActionType} action)");
        StopAction(ent);
    }

    /// <summary>
    /// Initiates an action upon a target entity by the specified console.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <param name="actionType">The action to be enacted on the target.</param>
    private void StartAction(Entity<StationAiFixerConsoleComponent> ent, StationAiFixerConsoleAction actionType)
    {
        if (IsActionInProgress(ent))
        {
            StopAction(ent);
        }

        if (IsTargetValid(ent, actionType))
        {
            var duration = actionType == StationAiFixerConsoleAction.Repair ?
                ent.Comp.RepairDuration :
                ent.Comp.PurgeDuration;

            ent.Comp.ActionType = actionType;
            ent.Comp.ActionStartTime = _timing.CurTime;
            ent.Comp.ActionEndTime = _timing.CurTime + duration;
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
        Dirty(ent);

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Finalizes the action being conducted by the specified console
    /// (i.e., repairing or purging a target).
    /// </summary>
    /// <param name="ent">The console.</param>
    protected virtual void FinalizeAction(Entity<StationAiFixerConsoleComponent> ent)
    {
        if (IsActionInProgress(ent) && ent.Comp.ActionTarget != null)
        {
            if (ent.Comp.ActionType == StationAiFixerConsoleAction.Repair)
            {
                _mobState.ChangeMobState(ent.Comp.ActionTarget.Value, MobState.Alive);
            }
            else if (ent.Comp.ActionType == StationAiFixerConsoleAction.Purge &&
                TryGetStationAiHolder(ent, out var holder))
            {
                _container.RemoveEntity(holder.Value, ent.Comp.ActionTarget.Value, force: true);
                PredictedQueueDel(ent.Comp.ActionTarget);

                ent.Comp.ActionTarget = null;
                Dirty(ent);
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
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (IsActionInProgress(ent))
        {
            var currentStage = ent.Comp.ActionType + ent.Comp.CurrentActionStage.ToString();

            if (!_appearance.TryGetData(ent, StationAiFixerConsoleVisuals.Key, out string oldStage, appearance) ||
                oldStage != currentStage)
            {
                _appearance.SetData(ent, StationAiFixerConsoleVisuals.Key, currentStage, appearance);
            }

            return;
        }

        var target = ent.Comp.ActionTarget;
        var state = StationAiState.Empty;

        if (TryComp<StationAiCustomizationComponent>(target, out var customization) && !EntityManager.IsQueuedForDeletion(target.Value))
        {
            state = customization.State;
        }

        _appearance.SetData(ent, StationAiFixerConsoleVisuals.Key, state.ToString(), appearance);
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
    /// <returns>True if a valid target was found.</returns>
    public bool TryGetTarget(Entity<StationAiFixerConsoleComponent> ent, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;

        if (!TryGetStationAiHolder(ent, out var holder))
            return false;

        if (!_container.TryGetContainer(holder.Value, ent.Comp.StationAiMindSlot, out var stationAiMindSlot) || stationAiMindSlot.Count == 0)
            return false;

        var stationAi = stationAiMindSlot.ContainedEntities[0];

        if (!HasComp<MobStateComponent>(stationAi))
            return false;

        target = stationAi;

        return !EntityManager.IsQueuedForDeletion(target.Value);
    }

    /// <summary>
    /// Try to find a station AI holder being stored inside the specified console.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <param name="holder">The found holder.</param>
    /// <returns>True if a valid holder was found.</returns>
    public bool TryGetStationAiHolder(Entity<StationAiFixerConsoleComponent> ent, [NotNullWhen(true)] out EntityUid? holder)
    {
        holder = null;

        if (!_container.TryGetContainer(ent, ent.Comp.StationAiHolderSlot, out var holderContainer) ||
            holderContainer.Count == 0)
        {
            return false;
        }

        holder = holderContainer.ContainedEntities[0];

        return true;
    }

    /// <summary>
    /// Determines if the specified console can act upon its action target.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <param name="actionType">The action to be enacted on the target.</param>
    /// <returns>True, if the target is valid for the specified console action.</returns>
    public bool IsTargetValid(Entity<StationAiFixerConsoleComponent> ent, StationAiFixerConsoleAction actionType)
    {
        if (ent.Comp.ActionTarget == null)
            return false;

        if (actionType == StationAiFixerConsoleAction.Purge)
            return true;

        if (actionType == StationAiFixerConsoleAction.Repair &&
            _mobState.IsDead(ent.Comp.ActionTarget.Value))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns whether an station AI holder is inserted into the specified console.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <returns>True if a station AI holder is inserted.</returns>
    public bool IsStationAiHolderInserted(Entity<StationAiFixerConsoleComponent> ent)
    {
        return TryGetStationAiHolder(ent, out var _);
    }

    /// <summary>
    /// Returns whether the specified console has an action in progress.
    /// </summary>
    /// <param name="ent">The console.</param>
    /// <returns>Ture, if an action is in progress.</returns>
    public bool IsActionInProgress(Entity<StationAiFixerConsoleComponent> ent)
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
