using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Trigger.Components.Conditions;

namespace Content.Shared.Trigger.Systems.Conditions;

public sealed class MindRoleTriggerConditionSystem : TriggerConditionSystem<MindRoleTriggerConditionComponent>
{
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    protected override void CheckCondition(Entity<MindRoleTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (ent.Comp.EntityWhitelist != null)
        {
            if (!_mind.TryGetMind(ent.Owner, out var entMindId, out var entMindComp))
            {
                ModifyEvent(ent, true, ref args); // the entity has no mind
                return;
            }

            if (!_role.MindHasRole((entMindId, entMindComp), ent.Comp.EntityWhitelist))
            {
                ModifyEvent(ent, true, ref args); // the entity does not have the required role
                return;
            }
        }

        if (ent.Comp.UserWhitelist != null)
        {
            if (args.User == null || !_mind.TryGetMind(args.User.Value, out var userMindId, out var userMindComp))
            {
                ModifyEvent(ent, true, ref args); // no user or the user has no mind
                return;
            }

            if (!_role.MindHasRole((userMindId, userMindComp), ent.Comp.UserWhitelist))
            {
                ModifyEvent(ent, true, ref args); // the user does not have the required role
            }
        }
    }
}
