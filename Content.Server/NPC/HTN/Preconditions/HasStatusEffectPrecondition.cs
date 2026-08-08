using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if entity have specified status effect
/// </summary>
public sealed partial class HasStatusEffectPrecondition : HTNPrecondition
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    [DataField(required: true)]
    public EntProtoId StatusEffect;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return _statusEffects.HasStatusEffect(owner, StatusEffect);
    }
}
