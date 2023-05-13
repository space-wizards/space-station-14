using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Content.Server.Speech.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class MakeSentient : ReagentEffect
{
    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;

        // This piece of code makes things able to speak "normally". One thing of note is that monkeys have a unique accent and won't be affected by this.
        entityManager.RemoveComponent<ReplacementAccentComponent>(uid);

        // Monke talk. This makes cognizine a cure to AMIV's long term damage funnily enough, do with this information what you will.
        entityManager.RemoveComponent<MonkeyAccentComponent>(uid);

        // This makes it so it doesn't add a ghost role to things that are already sentient
        if (entityManager.HasComponent<MindComponent>(uid))
        {
            return;
        }

        // No idea what anything past this point does
        if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole) ||
            entityManager.TryGetComponent(uid, out GhostTakeoverAvailableComponent? takeOver))
        {
            return;
        }

        ghostRole = entityManager.AddComponent<GhostRoleComponent>(uid);
        entityManager.AddComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
        ghostRole.RoleName = entityData.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-cognizine-description");
    }
}

// Original code written by areitpog on GitHub in Issue #7666, then I (Interrobang01) copied it and used my nonexistant C# skills to try to make it work again.
