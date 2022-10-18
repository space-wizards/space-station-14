using System;

//using Content.Server.AI.Components; // This was in the original code but caused an error so I'm gonna keep it as a comment just in case
using Content.Server.Mind.Components;
using Content.Server.NPC.Components; // Don't think I actually need this but keeping it just in case
using Content.Shared.Emoting;
using Content.Shared.Examine;
using Content.Shared.Movement.Components;
using Content.Shared.Speech;
using Content.Shared.Damage;

using Content.Shared.Chemistry.Reagent;
using Content.Server.Ghost.Roles.Components;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class MakeSentient : ReagentEffect
{
    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;

        // This makes it so it doesn't affect things that are already sentient
        if (entityManager.HasComponent<MindComponent>(uid))
        {
            return;
        }

        // This removes the ActiveNPCComponent, because I'm pretty sure that gets rid of the preexisting AI which we probably don't want
        // Any glitches found in this code probably stem from this part
        entityManager.RemoveComponent<ActiveNPCComponent>(uid);
        
        // This piece of code makes things able to speak "normally". One thing of note is that monkeys have a unique accent and won't be affected by this.
        entityManager.RemoveComponent<ReplacementAccentComponent>(uid);

        entityManager.EnsureComponent<MindComponent>(uid);
        //entityManager.EnsureComponent<SharedPlayerInputMoverComponent>(uid); // No idea what this line was for, but it caused errors and the code works fine without them
        //entityManager.EnsureComponent<SharedPlayerMobMoverComponent>(uid); // No idea what this line was for, but it caused errors and the code works fine without them
        entityManager.EnsureComponent<SharedSpeechComponent>(uid);
        entityManager.EnsureComponent<SharedEmotingComponent>(uid);
        entityManager.EnsureComponent<ExaminerComponent>(uid);

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