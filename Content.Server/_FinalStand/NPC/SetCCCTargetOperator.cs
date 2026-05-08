using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;

namespace Content.Server._FinalStand.NPC;

// code to make enemies beeline to ccc, attempt to ignore distance and los checks
public sealed partial class SetCCCTargetOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.CurrentOrderedTarget, out var target, _entManager)
            || !_entManager.EntityExists(target))
            return (false, null);

        return (true, new Dictionary<string, object>
        {
            { "Target", target },
            { "TargetCoordinates", new EntityCoordinates(target, Vector2.Zero) },
        });
    }
}
