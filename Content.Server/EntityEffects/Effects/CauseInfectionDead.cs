// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class CauseInfectionDead : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-cause-infection-dead", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;

        if (entityManager.HasComponent<NecromorfComponent>(args.TargetEntity))
            return;

        if (entityManager.HasComponent<ImmunitetInfectionDeadComponent>(args.TargetEntity))
            return;

        if (entityManager.HasComponent<MobStateComponent>(args.TargetEntity)
            && entityManager.HasComponent<NecromorfAfterInfectionComponent>(args.TargetEntity))
            entityManager.EnsureComponent<InfectionDeadComponent>(args.TargetEntity);

        entityManager.EnsureComponent<InfectionDeadComponent>(args.TargetEntity);
    }
}

