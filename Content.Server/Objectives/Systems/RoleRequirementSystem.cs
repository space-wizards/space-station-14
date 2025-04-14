using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles role requirement for objectives that require a certain (probably antagonist) role(s).
/// </summary>
public sealed class RoleRequirementSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, RoleRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var role in comp.Roles)
        {
            if (!EntityManager.ComponentFactory.TryGetRegistration(role, out var roleReg))
            {
                Log.Error($"Role component not found for RoleRequirementComponent: {role}");
                continue;
            }

            if (_roles.MindHasRole(args.MindId, roleReg.Type, out _))
                return; // whitelist pass
        }

        // whitelist fail
        args.Cancelled = true;
    }
}
