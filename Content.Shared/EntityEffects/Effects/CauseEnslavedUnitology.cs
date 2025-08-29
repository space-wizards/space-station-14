// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.DeadSpace.Necromorphs.Sanity;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.Damage;
using Content.Shared.Zombies;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class CauseEnslavedUnitology : EventEntityEffect<CauseEnslavedUnitology>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cause-enslave", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.HasComponent<MobStateComponent>(args.TargetEntity)
            || !args.EntityManager.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity))
        {
            return;
        }

        if (args.EntityManager.HasComponent<ImmunitetInfectionDeadComponent>(args.TargetEntity))
        {
            DamageSpecifier dspec = new();
            dspec.DamageDict.Add("Cellular", 5f);
            args.EntityManager.System<DamageableSystem>().TryChangeDamage(args.TargetEntity, dspec, true, false);
            return;
        }

        if (args.EntityManager.HasComponent<UnitologyComponent>(args.TargetEntity)
            || args.EntityManager.HasComponent<UnitologyEnslavedComponent>(args.TargetEntity)
            || args.EntityManager.HasComponent<NecromorfComponent>(args.TargetEntity)
            || args.EntityManager.HasComponent<ZombieComponent>(args.TargetEntity)
            || !args.EntityManager.HasComponent<SanityComponent>(args.TargetEntity))
            return;

        args.EntityManager.RemoveComponent<InfectionDeadComponent>(args.TargetEntity);
        args.EntityManager.EnsureComponent<UnitologyEnslavedComponent>(args.TargetEntity);
    }
}
