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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionUpgradeComponent, ActionUpgradeEvent>(OnActionUpgradeEvent);
    }

    private void OnActionUpgradeEvent(EntityUid uid, ActionUpgradeComponent component, ActionUpgradeEvent args)
    {
        // TODO: Add level (check if able first)
        if (!CanLevelUp(args.NewLevel, component.EffectedLevels, out var newActionProto))
            return;

        component.Level = args.NewLevel;
        // TODO: Replace current action with new one

        // TODO: Preserve ordering of actions
    }

    private bool CanLevelUp
        (int newLevel,
         Dictionary<int, string> levelDict,
        [NotNullWhen(true)]out string? newLevelProto)
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
    /// <param name="actionId"></param>
    /// <param name="actionUpgradeComponent"></param>
    public void UpgradeAction(EntityUid? actionId, ActionUpgradeComponent? actionUpgradeComponent = null)
    {
        if (!TryGetActionUpgrade(actionId, out var actionUpgradeComp))
            return;

        if (actionUpgradeComponent == null)
            actionUpgradeComponent = actionUpgradeComp;

        DebugTools.AssertNotNull(actionUpgradeComponent);
        DebugTools.AssertNotNull(actionId);

        var newLevel = actionUpgradeComponent.Level++;
        RaiseActionUpgradeEvent(newLevel, actionId.Value);
    }

    /// <summary>
    ///     Sets an upgrade to the specified level
    /// </summary>
    /// <param name="actionId"></param>
    /// <param name="newLevel"></param>
    /// <param name="actionUpgradeComponent"></param>
    private void UpgradeAction(EntityUid? actionId, int newLevel, ActionUpgradeComponent? actionUpgradeComponent = null)
    {
        if (!TryGetActionUpgrade(actionId, out var actionUpgradeComp))
            return;

        if (actionUpgradeComponent == null)
            actionUpgradeComponent = actionUpgradeComp;

        DebugTools.AssertNotNull(actionUpgradeComponent);
        DebugTools.AssertNotNull(actionId);

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
