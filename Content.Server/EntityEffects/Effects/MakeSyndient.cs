using Content.Server.Ghost.Roles.Components;
using Content.Server.Speech.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class MakeSyndient : EntityEffect
{
    //this is basically completely copied from MakeSentient, but with a bit of changes to how the ghost roles are listed
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-sentient", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;

        // Let affected entities speak normally to make this effect different from, say, the "random sentience" event
        // This also works on entities that already have a mind
        // We call this before the mind check to allow things like player-controlled mice to be able to benefit from the effect
        entityManager.RemoveComponent<ReplacementAccentComponent>(uid);
        entityManager.RemoveComponent<MonkeyAccentComponent>(uid);

        // Stops from adding a ghost role to things like people who already have a mind
        if (entityManager.TryGetComponent<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
        {
            return;
        }

        //slightly hacky way to make sure it doesn't work on humanoid ghost roles that haven't been claimed yet
        if (entityManager.TryGetComponent<HumanoidAppearanceComponent>(uid, out HumanoidAppearanceComponent? component))
        {
            return;
        }

        // Don't add a ghost role to things that already have ghost roles
        if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
        {
            
            //change the role description and rules to make it clear it's been injected with syndizine
            ghostRole = entityManager.GetComponent<GhostRoleComponent>(uid);
            ghostRole.RoleDescription = Loc.GetString("ghost-role-information-syndizine-description");
            ghostRole.RoleRules = Loc.GetString("ghost-role-information-familiar-rules");
            return;
        }

        ghostRole = entityManager.AddComponent<GhostRoleComponent>(uid);
        entityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
        ghostRole.RoleName = entityData.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-syndizine-description");
        ghostRole.RoleRules = Loc.GetString("ghost-role-information-familiar-rules");
    }
}
