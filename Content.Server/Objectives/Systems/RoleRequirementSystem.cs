using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles role requirement for objectives that require a certain (probably antagonist) role(s).
/// </summary>
public sealed class RoleRequirementSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, RoleRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        // this whitelist trick only works because roles are components on the mind and not entities
        // if that gets reworked then this will need changing
        if (!comp.Roles.IsValid(args.MindId, EntityManager))
            args.Cancelled = true;
    }
}
