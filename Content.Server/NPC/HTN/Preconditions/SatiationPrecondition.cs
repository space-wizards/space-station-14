using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// IsMet if has a satiation component whose satiation for <see cref="BaseSatiationPrecondition.SatiationType"/> meets
/// the criteria specified by <see cref="Above"/> and <see cref="Below"/>.
/// </summary>
public sealed partial class BaseSatiationPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField]
    public SatiationValue? Above;

    [DataField]
    public SatiationValue? Below;

    [DataField]
    public ProtoId<SatiationTypePrototype> SatiationType;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager) ||
            !_entManager.TryGetComponent<SatiationComponent>(owner, out var satiation))
            return false;

        return _entManager.System<SatiationSystem>()
            .IsValueInRange(
                (owner, satiation),
                SatiationType,
                Above,
                Below
            );
    }
}
