using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if entity have specified status effect
/// </summary>
public sealed partial class HasStatusEffectPrecondition : HTNPrecondition
{
    private StatusEffectsSystem _statusEffects = default!;

    [DataField(required: true)]
    public EntProtoId StatusEffect;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _statusEffects =  sysManager.GetEntitySystem<StatusEffectsSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return _statusEffects.HasStatusEffect(owner, StatusEffect);
    }
}
