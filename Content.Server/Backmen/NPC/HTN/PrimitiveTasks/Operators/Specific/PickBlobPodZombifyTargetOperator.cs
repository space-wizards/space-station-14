using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;

namespace Content.Server.Backmen.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class PickBlobPodZombifyTargetOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private NpcFactionSystem _factions = default!;
    private MobStateSystem _mobSystem = default!;

    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;

    [DataField("rangeKey", required: true)]
    public string RangeKey = string.Empty;

    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    [DataField("zombifyKey")]
    public string ZombifyKey = string.Empty;

    /// <summary>
    /// Where the pathfinding result will be stored (if applicable). This gets removed after execution.
    /// </summary>
    [DataField("pathfindKey")]
    public string PathfindKey = NPCBlackboard.PathfindKey;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _mobSystem = sysManager.GetEntitySystem<MobStateSystem>();
        _factions = sysManager.GetEntitySystem<NpcFactionSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entManager))
            return (false, null);

        var huAppQuery = _entManager.GetEntityQuery<HumanoidAppearanceComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        var targets = new List<EntityUid>();

        foreach (var entity in _factions.GetNearbyHostiles(owner, range))
        {
            if (!huAppQuery.TryGetComponent(entity, out var humanoidAppearance))
                continue;

            if (_mobSystem.IsAlive(entity))
                continue;

            targets.Add(entity);
        }

        foreach (var target in targets)
        {
            if (!xformQuery.TryGetComponent(target, out var xform))
                continue;

            var targetCoords = xform.Coordinates;
            var path = await _pathfinding.GetPath(owner, target, range, cancelToken);
            if (path.Result != PathResult.Path)
            {
                continue;
            }

            return (true, new Dictionary<string, object>()
            {
                { TargetKey, targetCoords },
                { ZombifyKey, target },
                { PathfindKey, path}
            });
        }

        return (false, null);
    }
}
