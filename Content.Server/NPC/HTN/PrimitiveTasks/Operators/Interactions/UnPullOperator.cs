using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

public sealed partial class UnPullOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SharedPullingSystem _pulling = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _pulling = sysManager.GetEntitySystem<SharedPullingSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken token)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_pulling.IsPulled(owner) ||
            !_entManager.TryGetComponent<SharedPullableComponent>(owner, out var pullable))
            return (false, null);

        return (_pulling.TryStopPull(pullable), null);
    }

}
