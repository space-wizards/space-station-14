using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Actions.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Actions;

public sealed class ActionUpgradeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionUpgradeComponent, ActionUpgradeEvent>(OnActionUpgradeEvent);
    }

    private void OnActionUpgradeEvent(EntityUid uid, ActionUpgradeComponent component, ActionUpgradeEvent args)
    {
        if (!CanLevelUp(args.NewLevel, component.EffectedLevels, out var newActionProto)
            || !_actions.TryGetActionData(uid, out var actionComp))
            return;

        var originalContainer = actionComp.Container;
        var originalAttachedEntity = actionComp.AttachedEntity;

        _actionContainer.RemoveAction(uid, actionComp);

        EntityUid? upgradedActionId = null;
        if (originalContainer != null
            && TryComp<ActionsContainerComponent>(originalContainer.Value, out var actionContainerComp))
        {
            upgradedActionId = _actionContainer.AddAction(originalContainer.Value, newActionProto, actionContainerComp);

            if (originalAttachedEntity != null)
                _actions.GrantContainedActions(originalAttachedEntity.Value, originalContainer.Value);
            else
                _actions.GrantContainedActions(originalContainer.Value, originalContainer.Value);
        }
        else if (originalAttachedEntity != null)
        {
            upgradedActionId = _actionContainer.AddAction(originalAttachedEntity.Value, newActionProto);
        }

        if (!TryComp<ActionUpgradeComponent>(upgradedActionId, out var upgradeComp))
            return;

        upgradeComp.Level = args.NewLevel;

        // TODO: Preserve ordering of actions

        _entityManager.DeleteEntity(uid);
    }

    public bool TryUpgradeAction(EntityUid? actionId, ActionUpgradeComponent? actionUpgradeComponent = null, int newLevel = 0)
    {
        if (!TryGetActionUpgrade(actionId, out var actionUpgradeComp))
            return false;

        actionUpgradeComponent ??= actionUpgradeComp;
        DebugTools.AssertNotNull(actionUpgradeComponent);
        DebugTools.AssertNotNull(actionId);

        if (newLevel < 1)
            newLevel = actionUpgradeComponent.Level + 1;

        if (!CanLevelUp(newLevel, actionUpgradeComponent.EffectedLevels, out _))
            return false;

        UpgradeAction(actionId, actionUpgradeComp);
        return true;
    }

    // TODO: Add checks for branching upgrades
    private bool CanLevelUp(
        int newLevel,
        Dictionary<int, EntProtoId> levelDict,
        [NotNullWhen(true)]out EntProtoId? newLevelProto)
    {
        newLevelProto = null;

        if (levelDict.Count < 1)
            return false;

        var canLevel = false;
        var finalLevel = levelDict.Keys.ToList()[levelDict.Keys.Count - 1];

        foreach (var (level, proto) in levelDict)
        {
            if (newLevel != level || newLevel > finalLevel)
                continue;

            canLevel = true;
            newLevelProto = proto;
            DebugTools.AssertNotNull(newLevelProto);
            break;
        }

        return canLevel;
    }

    /// <summary>
    ///     Raises a level by one
    /// </summary>
    public void UpgradeAction(EntityUid? actionId, ActionUpgradeComponent? actionUpgradeComponent = null, int newLevel = 0)
    {
        if (!TryGetActionUpgrade(actionId, out var actionUpgradeComp))
            return;

        actionUpgradeComponent ??= actionUpgradeComp;
        DebugTools.AssertNotNull(actionUpgradeComponent);
        DebugTools.AssertNotNull(actionId);

        if (newLevel < 1)
            newLevel = actionUpgradeComponent.Level + 1;

        RaiseActionUpgradeEvent(newLevel, actionId.Value);
    }

    private void RaiseActionUpgradeEvent(int level, EntityUid actionId)
    {
        var ev = new ActionUpgradeEvent(level, actionId);
        RaiseLocalEvent(actionId, ev);
    }

    public bool TryGetActionUpgrade(
        [NotNullWhen(true)] EntityUid? uid,
        [NotNullWhen(true)] out ActionUpgradeComponent? result,
        bool logError = true)
    {
        result = null;
        if (!Exists(uid))
            return false;

        if (!TryComp<ActionUpgradeComponent>(uid, out var actionUpgradeComponent))
        {
            Log.Error($"Failed to get action upgrade from action entity: {ToPrettyString(uid.Value)}");
            return false;
        }

        result = actionUpgradeComponent;
        DebugTools.AssertOwner(uid, result);
        return true;
    }
}
