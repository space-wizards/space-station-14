using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class MakeGhostRole : ReagentEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-ghost-role", ("chance", Probability));

    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;

        // If the entity already has a ghost role, don't add another one.
        if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole) && !ghostRole.Taken)
        {
            return;
        }

        // If the entity has a mind, kick them out of it. This is done because the Ghost role cannot be taken when the entity has a mind.
        var minds = entityManager.System<SharedMindSystem>();
        if (minds.TryGetMind(uid, out var mindId, out var mind))
        {
            entityManager.System<GameTicker>().OnGhostAttempt(mindId, false, false, mind);
        }

        // If the entity has a ghost role, remove it. This is done, because ReregisterOnGhost is set to false.
        // Without this, the entity would not show up in the ghost role menu if the amnesia reagent effect is applied again.
        // Removing the ghost role component removes it in the GhostRoleSystem, meaning it will be re-added when the amnesia reagent effect is applied again.
        if (entityManager.TryGetComponent(uid, out ghostRole))
        {
            entityManager.RemoveComponent<GhostRoleComponent>(uid);
        }

        ghostRole = entityManager.EnsureComponent<GhostRoleComponent>(uid);
        entityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
        ghostRole.RoleName = entityData.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-amnesia-description");
        // This is done so that doing /ghost will not show the entity in the ghost role menu.
        ghostRole.ReregisterOnGhost = false;
    }
}
