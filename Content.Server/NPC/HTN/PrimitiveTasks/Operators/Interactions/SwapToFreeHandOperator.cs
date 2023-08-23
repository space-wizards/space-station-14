using System.Threading;
using System.Threading.Tasks;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;


/// <summary>
/// Swaps to any free hand.
/// </summary>
public sealed partial class SwapToFreeHandOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<List<string>>(NPCBlackboard.FreeHands, out var hands, _entManager) ||
            !_entManager.TryGetComponent<HandsComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner), out var handsComp))
        {
            return (false, null);
        }

        foreach (var hand in hands)
        {
            return (true, new Dictionary<string, object>()
            {
                {
                    NPCBlackboard.ActiveHand, handsComp.Hands[hand]
                },
                {
                    NPCBlackboard.ActiveHandFree, true
                },
            });
        }

        return (false, null);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        // TODO: Need interaction cooldown
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var handSystem = _entManager.System<HandsSystem>();

        if (!handSystem.TrySelectEmptyHand(owner))
        {
            return HTNOperatorStatus.Failed;
        }

        return HTNOperatorStatus.Finished;
    }
}
