using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.EntityConditions;
using Content.Shared.Popups;

namespace Content.Shared.Actions;

public sealed partial class ActionRequiresConditionSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    [SubscribeLocalEvent]
    private void OnActionAttempt(Entity<ActionRequiresConditionComponent> ent, ref ActionAttemptEvent args)
    {
        if (!_conditions.TryConditions(ent.Owner, ent.Comp.Conditions))
        {
            if (ent.Comp.FailureMessage != null)
                args.Reason = Loc.GetString(ent.Comp.FailureMessage);

            args.Cancelled = true;
        }
    }
}
