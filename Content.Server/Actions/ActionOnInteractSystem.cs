using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Interaction;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Actions;

/// <summary>
///     This System handled interactions for the <see cref="ActionOnInteractComponent"/>.
/// </summary>
public sealed class ActionOnInteractSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionOnInteractComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ActionOnInteractComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnActivate(EntityUid uid, ActionOnInteractComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || component.ActivateActions == null)
            return;

        var options = new List<InstantAction>();
        foreach (var action in component.ActivateActions)
        {
            if (ValidAction(action))
                options.Add(action);
        }

        if (options.Count == 0)
            return;

        var act = _random.Pick(options);
        if (act.Event != null)
            act.Event.Performer = args.User;

        act.Provider = uid;
        _actions.PerformAction(args.User, null, act, act.Event, _timing.CurTime, false);
        args.Handled = true;
    }

    private void OnAfterInteract(EntityUid uid, ActionOnInteractComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        // First, try entity target actions
        if (args.Target != null && component.EntityActions != null)
        {
            var entOptions = new List<EntityTargetAction>();
            foreach (var action in component.EntityActions)
            {
                if (!ValidAction(action, args.CanReach))
                    continue;

                if (!_actions.ValidateEntityTarget(args.User, args.Target.Value, action))
                    continue;

                entOptions.Add(action);
            }

            if (entOptions.Count > 0)
            {
                var entAct = _random.Pick(entOptions);
                if (entAct.Event != null)
                {
                    entAct.Event.Performer = args.User;
                    entAct.Event.Target = args.Target.Value;
                }

                entAct.Provider = uid;
                _actions.PerformAction(args.User, null, entAct, entAct.Event, _timing.CurTime, false);
                args.Handled = true;
                return;
            }
        }

        // else: try world target actions
        if (component.WorldActions == null)
            return;

        var options = new List<WorldTargetAction>();
        foreach (var action in component.WorldActions)
        {
            if (!ValidAction(action, args.CanReach))
                continue;

            if (!_actions.ValidateWorldTarget(args.User, args.ClickLocation, action))
                continue;

            options.Add(action);
        }

        if (options.Count == 0)
            return;

        var act = _random.Pick(options);
        if (act.Event != null)
        {
            act.Event.Performer = args.User;
            act.Event.Target = args.ClickLocation;
        }

        act.Provider = uid;
        _actions.PerformAction(args.User, null, act, act.Event, _timing.CurTime, false);
        args.Handled = true;
    }

    private bool ValidAction(ActionType act, bool canReach = true)
    {
        if (!act.Enabled)
            return false;

        if (act.Charges.HasValue && act.Charges <= 0)
            return false;

        var curTime = _timing.CurTime;
        if (act.Cooldown.HasValue && act.Cooldown.Value.End > curTime)
            return false;

        return canReach || act is TargetedAction { CheckCanAccess: false };
    }
}
