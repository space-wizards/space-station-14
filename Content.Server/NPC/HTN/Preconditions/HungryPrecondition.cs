using Content.Shared.Hands.Components;
using Content.Shared.Nutrition.Components;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if the active hand entity has the specified components.
/// </summary>
public sealed partial class HungryPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true)]
    public SatiationThreashold MinHungerState = SatiationThreashold.Desperate;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
        {
            return false;
        }

        return _entManager.TryGetComponent<HungerComponent>(owner, out var hunger) ? hunger.Satiation.CurrentThreshold <= MinHungerState : false;
    }
}
