using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
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
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionOnInteractComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ActionOnInteractComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ActionOnInteractComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, ActionOnInteractComponent component, MapInitEvent args)
    {
        if (component.Actions == null)
            return;

        var comp = EnsureComp<ActionsContainerComponent>(uid);
        foreach (var id in component.Actions)
        {
            _actionContainer.AddAction(uid, id, comp);
        }
    }

    private void OnActivate(EntityUid uid, ActionOnInteractComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (component.ActionEntities is not {} actionEnts)
        {
            if (!TryComp<ActionsContainerComponent>(uid,  out var actionsContainerComponent))
                return;

            actionEnts = actionsContainerComponent.Container.ContainedEntities.ToList();
        }

        var options = GetValidActions<InstantActionComponent>(actionEnts);
        if (options.Count == 0)
            return;

        var (actId, action, comp) = _random.Pick(options);
        _actions.PerformAction(args.User, null, actId, action, comp.Event, _timing.CurTime, false);
        args.Handled = true;
    }

    private void OnAfterInteract(EntityUid uid, ActionOnInteractComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (component.ActionEntities is not {} actionEnts)
        {
            if (!TryComp<ActionsContainerComponent>(uid,  out var actionsContainerComponent))
                return;

            actionEnts = actionsContainerComponent.Container.ContainedEntities.ToList();
        }

        // First, try entity target actions
        if (args.Target != null)
        {
            var entOptions = GetValidActions<EntityTargetActionComponent>(actionEnts, args.CanReach);
            for (var i = entOptions.Count - 1; i >= 0; i--)
            {
                var action = entOptions[i];
                if (!_actions.ValidateEntityTarget(args.User, args.Target.Value, (action, action.Comp2)))
                    entOptions.RemoveAt(i);
            }

            if (entOptions.Count > 0)
            {
                var (entActId, entBase, entAct) = _random.Pick(entOptions);
                if (entAct.Event is {} ev)
                    ev.Target = args.Target.Value;

                _actions.PerformAction(args.User, null, entActId, entBase, entAct.Event, _timing.CurTime, false);
                args.Handled = true;
                return;
            }
        }

        // else: try world target actions
        var options = GetValidActions<WorldTargetActionComponent>(component.ActionEntities, args.CanReach);
        for (var i = options.Count - 1; i >= 0; i--)
        {
            var action = options[i];
            if (!_actions.ValidateWorldTarget(args.User, args.ClickLocation, (action, action.Comp2)))
                options.RemoveAt(i);
        }

        if (options.Count == 0)
            return;

        var (actId, comp, world) = _random.Pick(options);
        if (world.Event is {} worldEv)
            worldEv.Target = args.ClickLocation;

        _actions.PerformAction(args.User, null, actId, comp, world.Event, _timing.CurTime, false);
        args.Handled = true;
    }

    private bool ValidAction(EntityUid uid, ActionComponent action, bool canReach = true)
    {
        if (!action.Enabled)
            return false;

        if (action.Charges.HasValue && action.Charges <= 0)
            return false;

        var curTime = _timing.CurTime;
        if (action.Cooldown.HasValue && action.Cooldown.Value.End > curTime)
            return false;

        return canReach || (TryComp<TargetActionComponent>(uid, out var target) && !target.CheckCanAccess);
    }

    private List<Entity<ActionComponent, T>> GetValidActions<T>(List<EntityUid>? actions, bool canReach = true) where T: Component
    {
        var valid = new List<Entity<ActionComponent, T>>();

        if (actions == null)
            return valid;

        foreach (var id in actions)
        {
            if (!_actions.TryGetActionData(id, out var action) ||
                !ValidAction(id, action, canReach) ||
                !TryComp<T>(id, out var comp))
            {
                continue;
            }

            valid.Add((id, action, comp));
        }

        return valid;
    }
}
