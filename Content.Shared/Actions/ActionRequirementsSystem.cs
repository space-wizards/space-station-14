using System.Linq;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.IdentityManagement;

namespace Content.Shared.Actions;

/// <summary> <see cref="ActionRequirementsComponent"/> </summary>
public sealed partial class ActionRequirementsSystem : EntitySystem
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnActionAttempt(Entity<ActionRequirementsComponent> ent, ref ActionAttemptEvent args)
    {
        var performerConditions = GetConditions(ent, ActionRequirementTarget.Performer);

        var user = args.User;

        // We always need to pass in some target to the Loc, it is probably safe to assume if there is no target we can just pass in the user.
        // Its just popup stuff anyway.
        var target = args.Target ?? args.User;

        if (!_conditions.TryConditions(args.User, performerConditions))
        {
            DoCancel(ent, target, user, ref args);
            return;
        }

        var targetConditions =  GetConditions(ent, ActionRequirementTarget.Target);

        if (args.Target != null && !_conditions.TryConditions(args.Target.Value, targetConditions))
        {
            DoCancel(ent, target, user, ref args);
        }
    }

    [SubscribeLocalEvent]
    private void OnActionPerformed(Entity<ActionRequirementsComponent> ent, ref ActionPerformedEvent args)
    {
        var performerEffects = GetEffects(ent, ActionRequirementTarget.Performer);

        var user = args.Performer;
        var target = args.Target;

        _effects.TryApplyEffects(user, performerEffects, user: user);

        if (target == null)
            return;

        var targetEffects = GetEffects(ent, ActionRequirementTarget.Target);

        _effects.TryApplyEffects(target.Value, targetEffects, user: user);
    }

    private EntityCondition[] GetConditions(Entity<ActionRequirementsComponent> ent, ActionRequirementTarget target)
    {
        var conditions = ent.Comp.Conditions
            .Where(x => x.Key.HasFlag(target))
            .SelectMany(x => x.Value);

        return conditions.ToArray();
    }

    private EntityEffect[] GetEffects(Entity<ActionRequirementsComponent> ent, ActionRequirementTarget target)
    {
        var conditions = ent.Comp.Effects
            .Where(x => x.Key.HasFlag(target))
            .SelectMany(x => x.Value);

        return conditions.ToArray();
    }

    private void DoCancel(Entity<ActionRequirementsComponent> ent, EntityUid target, EntityUid user, ref ActionAttemptEvent args)
    {
        if (ent.Comp.FailPopup != null)
        {
            args.Reason = Loc.GetString(
                ent.Comp.FailPopup,
                ("target", Identity.Name(target, EntityManager)),
                ("performer", Identity.Name(user, EntityManager)),
                ("action", ent)
            );
        }

        args.Type = ent.Comp.FailPopupType;
        args.Cancelled = true;
    }
}
