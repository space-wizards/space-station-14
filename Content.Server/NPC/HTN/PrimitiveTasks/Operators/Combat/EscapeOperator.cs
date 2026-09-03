using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.CombatMode;
using Content.Shared.Storage.Components;
using Robust.Server.Containers;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

public sealed partial class EscapeOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private IEntityManager _entManager = default!;
    private ContainerSystem _containerSystem = default!;
    private EntityStorageSystem _entityStorage = default!;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _containerSystem = sysManager.GetEntitySystem<ContainerSystem>();
        _entityStorage = sysManager.GetEntitySystem<EntityStorageSystem>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_containerSystem.TryGetContainingContainer(owner, out var container))
        {
            return;
        }

        var melee = _entManager.EnsureComponent<NPCMeleeCombatComponent>(owner);
        melee.MissChance = blackboard.GetValueOrDefault<float>(NPCBlackboard.MeleeMissChance, _entManager);
        melee.Target = container.Owner;
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_containerSystem.TryGetContainingContainer(owner, out var container))
        {
            return (false, null);
        }

        if (!_entManager.HasComponent<EntityStorageComponent>(container.Owner))
        {
            // We must be in a backpack or something that we can't open or attack to escape from.
            // It could be possible to mirror some of the Resist.EscapeInventorySystem logic in this case.
            return (false, null);
        }

        if (!_containerSystem.IsEntityInContainer(owner))
        {
            return (false, null);
        }

        return (true, null);
    }

    public void ConditionalShutdown(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        _entManager.System<SharedCombatModeSystem>().SetInCombatMode(owner, false);
        _entManager.RemoveComponent<NPCMeleeCombatComponent>(owner);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);

        ConditionalShutdown(blackboard);
    }

    public override void PlanShutdown(NPCBlackboard blackboard)
    {
        base.PlanShutdown(blackboard);

        ConditionalShutdown(blackboard);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        base.Update(blackboard, frameTime);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_containerSystem.TryGetContainingContainer(owner, out var container)
            || _entityStorage.TryOpenStorage(owner, container.Owner))
        {
            return HTNOperatorStatus.Finished;
        }

        // We failed to open it... Perhaps violence is the answer?
        if (!_entManager.TryGetComponent<NPCMeleeCombatComponent>(owner, out var combat))
        {
            return HTNOperatorStatus.Failed;
        }

        combat.Target = container.Owner;

        switch (combat.Status)
        {
            case CombatStatus.TargetOutOfRange:
            case CombatStatus.Normal:
                return HTNOperatorStatus.Continuing;
            default:
                return HTNOperatorStatus.Failed;
        }
    }
}
