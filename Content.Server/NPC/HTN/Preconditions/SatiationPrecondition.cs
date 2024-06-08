using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if the active hand entity has the specified components.
/// </summary>
public sealed partial class SatiationPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SatiationSystem _satiation = default!;

    [DataField(required: true)]
    public SatiationThreashold MinSatiationState = SatiationThreashold.Concerned;
    [DataField(required: true)]
    public ProtoId<SatiationTypePrototype> SatiationType = "hunger";


    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
        {
            return false;
        }

        return _satiation.IsCurrentSatiationBelowState((owner, null), SatiationType, MinSatiationState);
    }
}
