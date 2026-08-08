using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Bots;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class PickNearbyInjectableOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private MedibotSystem _medibot = default!;
    [Dependency] private PathfindingSystem _pathfinding = default!;

    [DataField("rangeKey")] public string RangeKey = NPCBlackboard.MedibotInjectRange;

    /// <summary>
    /// Target entity to inject
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [DataField("targetMoveKey", required: true)]
    public string TargetMoveKey = string.Empty;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out _, _entManager))
            return (false, null);

        if (!_entManager.TryGetComponent<MedibotComponent>(owner, out var medibot))
            return (false, null);

        if (!blackboard.TryGetValue<IEnumerable<KeyValuePair<EntityUid, float>>>("TargetList", out var patients, _entManager))
            return (false, null);

        foreach (var (entity, _) in patients)
        {
            if (!_medibot.CheckInjectable((owner, medibot), entity, true))
                continue;

            //Needed to make sure it doesn't sometimes stop right outside it's interaction range
            var pathRange = SharedInteractionSystem.InteractionRange - 1f;
            var path = await _pathfinding.GetPath(owner, entity, pathRange, cancelToken);

            if (path.Result == PathResult.NoPath)
                continue;

            return (true, new Dictionary<string, object>()
            {
                { TargetKey, entity },
                { TargetMoveKey, _entManager.GetComponent<TransformComponent>(entity).Coordinates },
                { NPCBlackboard.PathfindKey, path },
            });
        }

        return (false, null);
    }
}
