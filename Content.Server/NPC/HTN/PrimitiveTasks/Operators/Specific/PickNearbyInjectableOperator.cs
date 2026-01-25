using System.Threading;
using System.Threading.Tasks;
using Content.Shared.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Bots;
using Content.Shared.Emag.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class PickNearbyInjectableOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private MedibotSystem _medibot = default!;
    private PathfindingSystem _pathfinding = default!;

    private EntityQuery<DamageableComponent> _damageQuery = default!;
    private EntityQuery<InjectableSolutionComponent> _injectQuery = default!;
    private EntityQuery<NPCRecentlyInjectedComponent> _recentlyInjected = default!;
    private EntityQuery<MobStateComponent> _mobState = default!;
    private EntityQuery<EmaggedComponent> _emaggedQuery = default!;

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
        _medibot = sysManager.GetEntitySystem<MedibotSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();

        _damageQuery = _entManager.GetEntityQuery<DamageableComponent>();
        _injectQuery = _entManager.GetEntityQuery<InjectableSolutionComponent>();
        _recentlyInjected = _entManager.GetEntityQuery<NPCRecentlyInjectedComponent>();
        _mobState = _entManager.GetEntityQuery<MobStateComponent>();
        _emaggedQuery = _entManager.GetEntityQuery<EmaggedComponent>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entManager))
            return (false, null);

        if (!_entManager.TryGetComponent<MedibotComponent>(owner, out var medibot))
            return (false, null);


        if (!blackboard.TryGetValue<IEnumerable<KeyValuePair<EntityUid, float>>>("TargetList", out var patients, _entManager))
            return (false, null);

        foreach (var (entity, _) in patients)
        {
            if (_mobState.TryGetComponent(entity, out var state) &&
                _injectQuery.HasComponent(entity) &&
                _damageQuery.TryGetComponent(entity, out var damage) &&
                !_recentlyInjected.HasComponent(entity))
            {
                // no treating dead bodies
                if (!_medibot.TryGetTreatment(medibot, state.CurrentState, out var treatment))
                    continue;

                // Only go towards a target if the bot can actually help them or if the medibot is emagged
                // note: this and the actual injecting don't check for specific damage types so for example,
                // radiation damage will trigger injection but the tricordrazine won't heal it.
                if (!_emaggedQuery.HasComponent(entity) && !treatment.IsValid(damage.TotalDamage))
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
