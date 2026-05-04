using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Whitelist;
using Robust.Server.Containers;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

/// <summary>
/// Queries for nearby entities matching a whitelist/blacklist, and then searches for a mob near to that entity.
/// </summary>
public sealed partial class PickEntityNearMobOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private ContainerSystem _container = default!;
    private EntityWhitelistSystem _entityWhitelist = default!;

    /// <summary>
    /// Range to search for entities
    /// </summary>
    [DataField(required: true)]
    public string RangeKey = default!;

    /// <summary>
    /// Target mob that was found near NearbyEntityTargetKey
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Target entity that was found
    /// </summary>
    [DataField(required: true)]
    public string NearbyEntityTargetKey = string.Empty;

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [DataField(required: true)]
    public string TargetMoveKey = string.Empty;

    /// <summary>
    /// Whitelist for what entities will get picked
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist for what entities will NOT get picked
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Range to search for mobs near the target entity
    /// </summary>
    [DataField(required: true)]
    public string MobRangeKey = default!;

    /// <summary>
    /// MobState to check for
    /// </summary>
    [DataField]
    public MobState? MobState;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _container = sysManager.GetEntitySystem<ContainerSystem>();
        _entityWhitelist = sysManager.GetEntitySystem<EntityWhitelistSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entManager))
            return (false, null);

        if (!blackboard.TryGetValue<float>(RangeKey, out var mobRange, _entManager))
            return (false, null);

        var mobState = _entManager.GetEntityQuery<MobStateComponent>();

        foreach (var entity in _lookup.GetEntitiesInRange(owner, range))
        {
            if (!_entityWhitelist.CheckBoth(entity, Blacklist, Whitelist))
                continue;

            //checking if there is anyone NEAR the entity we found
            foreach (var mob in _lookup.GetEntitiesInRange(entity, mobRange))
            {
                if (mob == owner)
                    continue;

                if (_container.IsEntityInContainer(mob))
                    continue;

                if (mobState.TryGetComponent(mob, out var state))
                {
                    if (MobState != null && state.CurrentState != MobState)
                        continue;

                    var pathRange = SharedInteractionSystem.InteractionRange;
                    var path = await _pathfinding.GetPath(owner, mob, pathRange, cancelToken);

                    if (path.Result == PathResult.NoPath)
                        return (false, null);

                    return (true, new Dictionary<string, object>()
                    {
                        {TargetKey, mob},
                        {NearbyEntityTargetKey, entity},
                        {TargetMoveKey, _entManager.GetComponent<TransformComponent>(mob).Coordinates},
                        {NPCBlackboard.PathfindKey, path},
                    });
                }
            }
        }

        return (false, null);
    }
}
