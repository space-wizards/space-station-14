using System;

using Content.Server.AI.Components;
using Content.Server.Mind.Components;
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

        if (entityManager.HasComponent<SharedPlayerMobMoverComponent>(uid))
        {
            return;
        }

        if (entityManager.HasComponent<AiControllerComponent>(uid))
            entityManager.RemoveComponent<AiControllerComponent>(uid);

        entityManager.EnsureComponent<MindComponent>(uid);
        entityManager.EnsureComponent<SharedPlayerInputMoverComponent>(uid);
        entityManager.EnsureComponent<SharedPlayerMobMoverComponent>(uid);
        entityManager.EnsureComponent<SharedSpeechComponent>(uid);
        entityManager.EnsureComponent<SharedEmotingComponent>(uid);
        entityManager.EnsureComponent<ExaminerComponent>(uid);

        if (entityManager.TryGetComponent(uid, out GhostTakeoverAvailableComponent? takeOver))
        {
            return;
        }

        takeOver = entityManager.AddComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
        takeOver.RoleName = entityData.EntityName;
        takeOver.RoleDescription = Loc.GetString("ghost-role-component-made-with-conscizine");
    }
}

