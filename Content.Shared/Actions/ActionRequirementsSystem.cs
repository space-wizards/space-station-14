using System.Linq;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.IdentityManagement;

namespace Content.Shared.Actions;

public sealed partial class ActionRequirementsSystem : EntitySystem
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnActionAttempt(Entity<ActionRequirementsComponent> ent, ref ActionAttemptEvent args)
    {
        var performerConditions = GetConditions(ent, ActionRequirementTarget.Performer);

        var user = args.User;
        var target = args.Target ?? args.User;

        if (!_conditions.TryConditions(args.User, performerConditions))
        {
            if (ent.Comp.FailPopup != null)
                args.Reason = Loc.GetString(ent.Comp.FailPopup, ("target", Identity.Name(target, EntityManager)), ("performer", Identity.Name(user, EntityManager)), ("action", ent));

            args.Type = ent.Comp.FailPopupType;
            args.Cancelled = true;
            return;
        }

        var targetConditions =  GetConditions(ent, ActionRequirementTarget.Target);

        if (args.Target != null && !_conditions.TryConditions(args.Target.Value, targetConditions))
        {
            if (ent.Comp.FailPopup != null)
                args.Reason = Loc.GetString(ent.Comp.FailPopup, ("target", Identity.Name(target, EntityManager)), ("performer", Identity.Name(user, EntityManager)), ("action", ent));

            args.Type = ent.Comp.FailPopupType;
            args.Cancelled = true;
        }
    }

    [SubscribeLocalEvent]
    private void OnActionPerformed(Entity<ActionRequirementsComponent> ent, ref ActionPerformedEvent args)
    {
        var performerEffects = GetEffects(ent, ActionRequirementTarget.Performer);

        var user = args.Performer;
        var target = args.Target;

        _effects.TryApplyEffects(user, performerEffects, user: user);

        var targetEffects =  GetEffects(ent, ActionRequirementTarget.Target);

        _effects.TryApplyEffects(target, targetEffects, user: user);
    }

    private EntityCondition[] GetConditions(Entity<ActionRequirementsComponent> ent, ActionRequirementTarget target)
    {
        // Death.
        var conditions = ent.Comp.Conditions
            .Where(x => x.Key.HasFlag(target))
            .SelectMany(x => x.Value);

        return conditions.ToArray();
    }

    private EntityEffect[] GetEffects(Entity<ActionRequirementsComponent> ent, ActionRequirementTarget target)
    {
        // Death.
        var conditions = ent.Comp.Effects
            .Where(x => x.Key.HasFlag(target))
            .SelectMany(x => x.Value);

        return conditions.ToArray();
    }
}
