using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// IsMet if has a satiation component whose satiation for <see cref="SatiationType"/> is at least
/// <see cref="MinSatiationState"/>.
/// </summary>
public sealed partial class SatiationPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true)]
    public SatiationThreshold MinSatiationState = SatiationThreshold.Desperate;

    [DataField]
    public ProtoId<SatiationTypePrototype> SatiationType;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
        {
            return false;
        }

        return _entManager.TryGetComponent<SatiationComponent>(owner, out var satiation) &&
               _entManager.System<SatiationSystem>().GetThresholdOrNull((owner, satiation), SatiationType) is
                   { } currentThreshold &&
               currentThreshold <= MinSatiationState;
    }
}
