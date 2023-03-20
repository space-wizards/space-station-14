using Content.Server.Mind.Components;
using Content.Server.Speech.Components;

using Content.Shared.Chemistry.Reagent;
using Content.Server.Ghost.Roles.Components;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class MakeSentient : ReagentEffect
{
    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;

        // This makes it so it doesn't add a ghost role to things that are already sentient
        if (entityManager.HasComponent<MindComponent>(uid))
        {
            return;
        }

        // No idea what anything past this point does
        if (entityManager.TryGetComponent(uid, out GhostTakeoverAvailableComponent? takeOver))
        {
            return;
        }

        takeOver = entityManager.AddComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
        takeOver.RoleName = entityData.EntityName;
        takeOver.RoleDescription = Loc.GetString("ghost-role-information-cognizine-description");
    }
}

// Original code written by areitpog on GitHub in Issue #7666, then I (Interrobang01) copied it and used my nonexistant C# skills to try to make it work again.
