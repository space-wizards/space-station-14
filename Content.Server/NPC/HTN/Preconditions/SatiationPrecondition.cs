using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// IsMet if has a satiation component whose satiation for <see cref="BaseSatiationPrecondition.SatiationType"/> meets
/// the criteria specified by <see cref="Above"/> and <see cref="Below"/>.
/// </summary>
/// <seealso cref="SatiationSystem.IsValueInRange"/>
public sealed partial class BaseSatiationPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    /// <summary>
    /// The bottom of the range the agent's satiation must be in for this condition to be met. If null, the range has
    /// no bottom.
    /// </summary>
    [DataField]
    public SatiationValue? Above;

    /// <summary>
    /// The top of the range the agent's satiation must be in for this condition to be met. If null, the range has no
    /// bottom.
    /// </summary>
    [DataField]
    public SatiationValue? Below;

    /// <summary>
    /// The type of the satiation considered by this condition. If the agent does not have this satiation type, this
    /// condition can never be met.
    /// </summary>
    [DataField]
    public ProtoId<SatiationTypePrototype> SatiationType;

    /// <inheritdoc/>
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
