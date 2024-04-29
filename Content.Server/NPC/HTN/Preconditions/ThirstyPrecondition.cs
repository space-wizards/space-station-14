using Content.Shared.Nutrition.Components;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if the active hand entity has the specified components.
/// </summary>
public sealed partial class ThirstyPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true)]
    public SatiationThreashold MinThirstState = SatiationThreashold.Desperate;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
        {
            return false;
        }

        return _entManager.TryGetComponent<SatiationComponent>(owner, out var satiation) ? satiation.Thirst.CurrentThreshold <= MinThirstState : false;
    }
}
