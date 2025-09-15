// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Content.Shared.Storage;
using Content.Shared.Body.Components;
using Content.Shared.DeadSpace.Abilities.Egg;
using Content.Shared.Mobs.Systems;
using Content.Shared.DeadSpace.Abilities.Egg.Components;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class CauseEgg : EventEntityEffect<CauseEgg>
{
    [DataField("spawned", required: true)]
    public List<EntitySpawnEntry> SpawnedEntities = new();

    [DataField]
    public float Duration = 60f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cause-egg", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var eggSystem = args.EntityManager.System<SharedEggSystem>();

        if (args.EntityManager.System<MobStateSystem>().IsDead(args.TargetEntity))
            return;

        if (!args.EntityManager.HasComponent<BodyComponent>(args.TargetEntity))
            return;

        if (!eggSystem.IsInfectPossible(args.TargetEntity))
            return;

        var egg = args.EntityManager.EnsureComponent<EggComponent>(args.TargetEntity);

        egg.SpawnedEntities = SpawnedEntities;
        eggSystem.Postpone(Duration, egg);
    }
}
