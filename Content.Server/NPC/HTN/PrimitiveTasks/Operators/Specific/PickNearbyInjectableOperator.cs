using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.NPC.Components;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed class PickNearbyInjectableOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private EntityLookupSystem _lookup = default!;

    [ViewVariables, DataField("rangeKey")] public string RangeKey = NPCBlackboard.MedibotInjectRange;

    /// <summary>
    /// Target entity to inject
    /// </summary>
    [ViewVariables, DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [ViewVariables, DataField("targetMoveKey", required: true)]
    public string TargetMoveKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out var range))
            return (false, null);

        var damageQuery = _entManager.GetEntityQuery<DamageableComponent>();
        var injectQuery = _entManager.GetEntityQuery<InjectableSolutionComponent>();
        var recentlyInjected = _entManager.GetEntityQuery<NPCRecentlyInjectedComponent>();
        var mobState = _entManager.GetEntityQuery<MobStateComponent>();

        foreach (var entity in _lookup.GetEntitiesInRange(owner, range))
        {
            if (mobState.HasComponent(entity) &&
                injectQuery.HasComponent(entity) &&
                damageQuery.TryGetComponent(entity, out var damage) &&
                damage.TotalDamage > 0 &&
                !recentlyInjected.HasComponent(entity))
            {
                return (true, new Dictionary<string, object>()
                {
                    {TargetKey, entity},
                    {TargetMoveKey, _entManager.GetComponent<TransformComponent>(entity).Coordinates}
                });
            }
        }

        return (false, null);
    }
}
