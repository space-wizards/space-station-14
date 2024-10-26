using System.Threading;
using System.Threading.Tasks;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Explosion.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

public sealed partial class StartTimerOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("idleKey")]
    public string IdleKey = "IdleTime";

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        return new(true, new Dictionary<string, object>()
        {
            { IdleKey, 1f }
        });
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var trigger = _entManager.System<TriggerSystem>();
        if (!_entManager.TryGetComponent<OnUseTimerTriggerComponent>(owner, out var timer))
            return HTNOperatorStatus.Failed;

        trigger.StartTimer((owner, timer), owner);
        blackboard.SetValue(IdleKey, timer.Delay);

        return HTNOperatorStatus.Finished;
    }
}
