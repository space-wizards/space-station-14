using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Hands.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed class SwapToFreeHandOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        // TODO: This no worky need to apply effects.
        if (!blackboard.TryGetValue<Dictionary<string, Hand>>(NPCBlackboard.FreeHands, out var hands, _entManager))
        {
            return (false, null);
        }

        foreach (var hand in hands.Values)
        {
            if (hand.IsEmpty)
                return (true, null);
        }

        return (false, null);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<Dictionary<string, Hand>>(NPCBlackboard.FreeHands, out var hands, _entManager))
        {
            return (false, null);
        }

        foreach (var hand in hands.Values)
        {
            if (hand.IsEmpty)
                return (true, null);
        }

        return (false, null);
    }
}
