using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Systems;
using Content.Shared.Silicons.StationAi;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind pool that uses <see cref="TargetSystem.AddAliveAi"/>.
/// </summary>
public sealed partial class AliveAiPool : MindPool
{
    public override void FindMinds(HashSet<Entity<MindComponent>> minds, EntityUid? exclude, IEntityManager entMan, TargetSystem targetSys)
    {
        var query = entMan.EntityQueryEnumerator<StationAiCoreComponent, StationAiHolderComponent>();
        var mindSys = entMan.System<SharedMindSystem>();
        var mobState = entMan.System<MobStateSystem>();
        var aiSys = entMan.System<SharedStationAiSystem>();
        while (query.MoveNext(out var uid, out _, out var aiHolder))
        {
            // the player needs to have a mind and not be the excluded one +
            // the player has to be alive
            if (!aiSys.TryGetHeld((uid, aiHolder), out var held) || mobState.IsDead(held.Value))
                continue;

            if (!mindSys.TryGetMind(held.Value, out var mind, out var mindComp) || mind == exclude)
                continue;

            minds.Add((mind, mindComp));
        }
    }
}
