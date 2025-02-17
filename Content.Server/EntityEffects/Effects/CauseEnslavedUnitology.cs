// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.DeadSpace.Necromorphs.Sanity;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Damage;
using Content.Shared.Zombies;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class CauseEnslavedUnitology : EntityEffect
{

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-cause-enslave", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        if (!entityManager.HasComponent<MobStateComponent>(args.TargetEntity)
            || !entityManager.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity))
        {
            return;
        }

        if (entityManager.HasComponent<ImmunitetInfectionDeadComponent>(args.TargetEntity)
            || entityManager.HasComponent<MindShieldComponent>(args.TargetEntity))
        {
            DamageSpecifier dspec = new();
            dspec.DamageDict.Add("Cellular", 5f);
            args.EntityManager.System<DamageableSystem>().TryChangeDamage(args.TargetEntity, dspec, true, false);
            return;
        }

        if (entityManager.HasComponent<UnitologyComponent>(args.TargetEntity)
            || entityManager.HasComponent<UnitologyEnslavedComponent>(args.TargetEntity)
            || entityManager.HasComponent<NecromorfComponent>(args.TargetEntity)
            || entityManager.HasComponent<ZombieComponent>(args.TargetEntity)
            || !entityManager.HasComponent<SanityComponent>(args.TargetEntity))
            return;

        entityManager.RemoveComponent<InfectionDeadComponent>(args.TargetEntity);
        entityManager.EnsureComponent<UnitologyEnslavedComponent>(args.TargetEntity);
    }
}

