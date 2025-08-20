using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if entity have specified status effect
/// </summary>
public sealed partial class HasStatusEffectPrecondition : HTNPrecondition
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true)]
    public EntProtoId StatusEffectId;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
        {
            return false;
        }

        return _statusEffectsSystem.HasStatusEffect(owner, StatusEffectId);
    }
}
