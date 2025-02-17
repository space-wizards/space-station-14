// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.DeadSpace.Abilities.Egg.Components;
using Content.Shared.Storage;
using Content.Shared.Mobs.Systems;
using Content.Shared.DeadSpace.Abilities.Egg;
using Content.Shared.Body.Components;
using Content.Shared.Zombies;
using Content.Server.Zombies;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class CauseEgg : EntityEffect
{
    [DataField("spawned", required: true)]
    public List<EntitySpawnEntry> SpawnedEntities = new();

    [DataField("duration")]
    public float Duration = 60f;
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-cause-egg", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;

        if (args.EntityManager.EntitySysManager.GetEntitySystem<MobStateSystem>().IsDead(target))
            return;

        if (!entityManager.HasComponent<BodyComponent>(target))
            return;

        if (entityManager.HasComponent<EggComponent>(target))
            return;

        if (entityManager.HasComponent<InfectionDeadComponent>(target) || entityManager.HasComponent<NecromorfComponent>(target))
            return;

        if (entityManager.HasComponent<ZombieComponent>(target) || entityManager.HasComponent<PendingZombieComponent>(target) || entityManager.HasComponent<ZombifyOnDeathComponent>(target))
            return;

        var eggSystem = args.EntityManager.EntitySysManager.GetEntitySystem<SharedEggSystem>();

        if (!eggSystem.IsInfectPossible(target))
            return;

        var egg = entityManager.EnsureComponent<EggComponent>(target);

        egg.SpawnedEntities = SpawnedEntities;
        eggSystem.Postpone(Duration, egg);

    }
}

