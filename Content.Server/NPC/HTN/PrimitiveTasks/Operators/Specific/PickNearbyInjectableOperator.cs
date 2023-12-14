using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Bots;
using Content.Shared.Emag.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class PickNearbyInjectableOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private EntityLookupSystem _lookup = default!;
    private MedibotSystem _medibot = default!;
    private PathfindingSystem _pathfinding = default!;

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

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _medibot = sysManager.GetEntitySystem<MedibotSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entManager))
            return (false, null);

        if (!_entManager.TryGetComponent<MedibotComponent>(owner, out var medibot))
            return (false, null);

        var damageQuery = _entManager.GetEntityQuery<DamageableComponent>();
        var injectQuery = _entManager.GetEntityQuery<InjectableSolutionComponent>();
        var recentlyInjected = _entManager.GetEntityQuery<NPCRecentlyInjectedComponent>();
        var mobState = _entManager.GetEntityQuery<MobStateComponent>();
        var emaggedQuery = _entManager.GetEntityQuery<EmaggedComponent>();

        foreach (var entity in _lookup.GetEntitiesInRange(owner, range))
        {
            if (mobState.TryGetComponent(entity, out var state) &&
                injectQuery.HasComponent(entity) &&
                damageQuery.TryGetComponent(entity, out var damage) &&
                !recentlyInjected.HasComponent(entity))
            {
                // no treating dead bodies
                if (!_medibot.TryGetTreatment(medibot, state.CurrentState, out var treatment))
                    continue;

                // Only go towards a target if the bot can actually help them or if the medibot is emagged
                // note: this and the actual injecting don't check for specific damage types so for example,
                // radiation damage will trigger injection but the tricordrazine won't heal it.
                if (!emaggedQuery.HasComponent(entity) && !treatment.IsValid(damage.TotalDamage))
                    continue;

                //Needed to make sure it doesn't sometimes stop right outside it's interaction range
                var pathRange = SharedInteractionSystem.InteractionRange - 1f;
                var path = await _pathfinding.GetPath(owner, entity, pathRange, cancelToken);

                if (path.Result == PathResult.NoPath)
                    continue;

                return (true, new Dictionary<string, object>()
                {
                    {TargetKey, entity},
                    {TargetMoveKey, _entManager.GetComponent<TransformComponent>(entity).Coordinates},
                    {NPCBlackboard.PathfindKey, path},
                });
            }
        }

        return (false, null);
    }
}
