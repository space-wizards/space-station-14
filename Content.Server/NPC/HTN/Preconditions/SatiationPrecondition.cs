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

    [DataField(required: true)]
    public SatiationThreashold MinSatiationState = SatiationThreashold.Concerned;
    [DataField(required: true)]
    public ProtoId<SatiationTypePrototype> SatiationType = "Hunger";


    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
        {
            return false;
        }

        return _entManager.System<SatiationSystem>().IsCurrentSatiationBelowState((owner, null), SatiationType, MinSatiationState);
    }
}
