using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if entity has the specified status effect
/// </summary>
public sealed partial class HasStatusEffectPrecondition : HTNPrecondition
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    [DataField(required: true)]
    public EntProtoId StatusEffect;

    /// <summary>
    /// Value returned if the entity does NOT have the specified status effect.
    /// Set this to 'true' to invert the return value - returning 'true' only if the effect is NOT present.
    /// </summary>
    [DataField("invert")]
    public bool Invert = false;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return _statusEffects.HasStatusEffect(owner, StatusEffect) ? !Invert : Invert;
    }
}
